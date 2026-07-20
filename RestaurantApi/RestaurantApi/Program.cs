using System.Text.Json.Serialization;
using RestaurantApi.Api;
using RestaurantApi.Configuration;
using RestaurantApi.Domain.Menu;
using RestaurantApi.Domain.Notifications;
using RestaurantApi.Domain.Orders;
using RestaurantApi.Domain.Pricing;
using RestaurantApi.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddSingleton<OrderRepository>();

builder.Services.AddSingleton<OrderService>();

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddSingleton<IMenuItemFactory, StarterFactory>();
builder.Services.AddSingleton<IMenuItemFactory, MainCourseFactory>();
builder.Services.AddSingleton<IMenuItemFactory, DessertFactory>();
builder.Services.AddSingleton<IMenuItemFactory, BeverageFactory>();
builder.Services.AddSingleton<IMenuItemFactoryProvider, MenuItemFactoryProvider>();

builder.Services.AddSingleton<MenuCatalog>();

builder.Services.AddSingleton<IPricingStrategy, StandardPricing>();
builder.Services.AddSingleton<IPricingStrategy, HappyHourPricing>();
builder.Services.AddSingleton<IPricingStrategy, GroupDiscountPricing>();
builder.Services.AddSingleton<IPricingStrategy, MenuFormulaPricing>();
builder.Services.AddSingleton<IPricingStrategyResolver, PricingStrategyResolver>();

builder.Services.AddSingleton(sp => RestaurantConfiguration.Initialize(
    sp.GetRequiredService<IMenuItemFactoryProvider>(),
    sp.GetRequiredService<MenuCatalog>()));

builder.Services.AddSingleton<IOrderSubject, OrderEventPublisher>();
builder.Services.AddSingleton<IOrderObserver, KitchenService>();
builder.Services.AddSingleton<IOrderObserver, DiningRoomService>();
builder.Services.AddSingleton<IOrderObserver, BillingService>();

var app = builder.Build();

var publieur = app.Services.GetRequiredService<IOrderSubject>();
foreach (var observateur in app.Services.GetServices<IOrderObserver>())
    publieur.Attach(observateur);

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => "Restaurant API is running. See /swagger for details.")
   .WithSummary("Verifie que l'API repond.");

app.MapGet("/api/menu", (RestaurantConfiguration config, TimeProvider horloge) =>
{
    var maintenant = TimeOnly.FromDateTime(horloge.GetLocalNow().DateTime);

    return Results.Ok(new MenuResponse(
        config.NomEtablissement,
        config.Devise,
        new OpeningHoursResponse(
            config.Horaires.Ouverture,
            config.Horaires.Fermeture,
            config.Horaires.EstOuvert(maintenant)),
        config.Menu
            .OrderBy(item => item.Category)
            .ThenBy(item => item.Name)
            .Select(MenuItemResponse.De)
            .ToArray()));
})
.WithSummary("Menu complet, horaires et informations de l'etablissement.")
.Produces<MenuResponse>();

app.MapGet("/api/menu/categories", (IMenuItemFactoryProvider fabriques) =>
    Results.Ok(fabriques.CategoriesDisponibles.OrderBy(c => c).ToArray()))
.WithSummary("Categories de plats acceptees par POST /api/menu.")
.WithDescription("La liste vient des fabriques enregistrees : elle se complete d'elle-meme quand on en ajoute une.")
.Produces<MenuCategory[]>();

app.MapPost("/api/menu", (
    CreateMenuItemRequest requete,
    IMenuItemFactoryProvider fabriques,
    MenuCatalog catalogue) =>
{
    if (!fabriques.CategoriesDisponibles.Contains(requete.Category))
        return Results.BadRequest(new ErrorResponse(
            $"Categorie inconnue : '{requete.Category}'.",
            fabriques.CategoriesDisponibles.Select(c => c.ToString()).ToArray()));

    if (string.IsNullOrWhiteSpace(requete.Id) || string.IsNullOrWhiteSpace(requete.Name))
        return Results.BadRequest(new ErrorResponse("L'identifiant et le nom sont obligatoires."));

    if (requete.Price <= 0)
        return Results.BadRequest(new ErrorResponse($"Prix invalide : {requete.Price}."));

    if (requete.PreparationTimeMinutes < 0)
        return Results.BadRequest(new ErrorResponse(
            $"Temps de preparation invalide : {requete.PreparationTimeMinutes}."));

    var plat = fabriques.Create(
        requete.Category, requete.Id.Trim(), requete.Name.Trim(),
        requete.Price, requete.PreparationTimeMinutes);

    if (!catalogue.Ajouter(plat))
        return Results.Conflict(new ErrorResponse($"Un plat porte deja l'identifiant '{plat.Id}'."));

    return Results.Created($"/api/menu/{plat.Id}", MenuItemResponse.De(plat));
})
.WithSummary("Cree un plat dans la categorie demandee (besoin 1).")
.WithDescription("Le type concret produit depend de la categorie : la fabrique correspondante s'en charge.")
.Produces<MenuItemResponse>(StatusCodes.Status201Created)
.Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
.Produces<ErrorResponse>(StatusCodes.Status409Conflict);

app.MapGet("/api/pricing-policies", (IPricingStrategyResolver resolver) =>
    Results.Ok(resolver.All.Select(s => new PricingPolicyResponse(s.Name, s.Description)).ToArray()))
.WithSummary("Politiques tarifaires disponibles.")
.Produces<PricingPolicyResponse[]>();

app.MapGet("/api/orders", (OrderRepository depot) =>
    Results.Ok(depot.GetAll().Select(c => OrderResponse.De(c)).ToArray()))
.WithSummary("Liste toutes les commandes.")
.Produces<OrderResponse[]>();

app.MapGet("/api/orders/{id}", (string id, OrderRepository depot) =>
    depot.GetById(id) is { } commande
        ? Results.Ok(OrderResponse.De(commande))
        : Results.NotFound(new ErrorResponse($"Commande '{id}' introuvable.")))
.WithSummary("Obtient une commande par son identifiant.")
.Produces<OrderResponse>()
.Produces<ErrorResponse>(StatusCodes.Status404NotFound);

app.MapPost("/api/orders", (CreateOrderRequest requete, OrderService service) =>
{
    var resultat = service.Creer(
        requete.TableNumber,
        requete.MenuItemIds ?? [],
        requete.PricingPolicy);

    return resultat.Reussi
        ? Results.Created($"/api/orders/{resultat.Order!.Id}",
            OrderResponse.De(resultat.Order, resultat.Message))
        : Results.BadRequest(new ErrorResponse(resultat.Erreur!));
})
.WithSummary("Cree une commande et calcule son prix selon la politique demandee.")
.Produces<OrderResponse>(StatusCodes.Status201Created)
.Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

app.MapPut("/api/orders/{id}/state", (string id, OrderService service) =>
{
    var resultat = service.FaireProgresser(id);

    if (resultat.Reussi)
        return Results.Ok(new OrderStateResponse(
            resultat.Order!.Id,
            resultat.Order.Status,
            resultat.Order.CanAdvance,
            resultat.Message!));

    return resultat.Raison switch
    {
        RaisonEchec.Introuvable => Results.NotFound(new ErrorResponse(resultat.Erreur!)),
        _ => Results.Conflict(new ErrorResponse(resultat.Erreur!))
    };
})
.WithSummary("Fait progresser la commande d'une etape dans son workflow.")
.WithDescription("Received -> InPreparation -> Ready -> Served -> Paid. Une commande payee renvoie 409.")
.Produces<OrderStateResponse>()
.Produces<ErrorResponse>(StatusCodes.Status404NotFound)
.Produces<ErrorResponse>(StatusCodes.Status409Conflict);

app.Run();
