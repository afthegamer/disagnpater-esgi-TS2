using RestaurantApi.Configuration;
using RestaurantApi.Domain.Menu;
using RestaurantApi.Domain.Notifications;
using RestaurantApi.Domain.Pricing;
using RestaurantApi.Repositories;

namespace RestaurantApi.Domain.Orders;

public enum RaisonEchec
{
    Introuvable,
    TransitionRefusee,
    EntreeInvalide
}

public sealed record OrderResult(Order? Order, string? Erreur, RaisonEchec? Raison, string? Message)
{
    public static OrderResult Ok(Order order, string? message = null) => new(order, null, null, message);

    public static OrderResult Echec(RaisonEchec raison, string erreur) => new(null, erreur, raison, null);

    public bool Reussi => Erreur is null;
}

public sealed class OrderService
{
    private const int MaxPlatsParCommande = 100;

    private readonly OrderRepository _depot;
    private readonly IPricingStrategyResolver _politiques;
    private readonly IOrderSubject _publieur;
    private readonly RestaurantConfiguration _configuration;
    private readonly TimeProvider _horloge;

    public OrderService(
        OrderRepository depot,
        IPricingStrategyResolver politiques,
        IOrderSubject publieur,
        RestaurantConfiguration configuration,
        TimeProvider horloge)
    {
        _depot = depot;
        _politiques = politiques;
        _publieur = publieur;
        _configuration = configuration;
        _horloge = horloge;
    }

    public OrderResult Creer(int numeroTable, IReadOnlyList<string> identifiantsPlats, string? politique)
    {
        if (numeroTable <= 0)
            return OrderResult.Echec(RaisonEchec.EntreeInvalide,
                $"Numero de table invalide : {numeroTable}.");

        if (identifiantsPlats.Count == 0)
            return OrderResult.Echec(RaisonEchec.EntreeInvalide,
                "Une commande doit contenir au moins un plat.");

        if (identifiantsPlats.Count > MaxPlatsParCommande)
            return OrderResult.Echec(RaisonEchec.EntreeInvalide,
                $"Commande trop volumineuse : {identifiantsPlats.Count} plats, maximum {MaxPlatsParCommande}.");

        var plats = new List<MenuItem>(identifiantsPlats.Count);
        foreach (var identifiant in identifiantsPlats)
        {
            var plat = _configuration.TrouverPlat(identifiant);
            if (plat is null)
                return OrderResult.Echec(RaisonEchec.EntreeInvalide,
                    $"Plat inconnu au menu : '{identifiant}'.");

            plats.Add(plat);
        }

        var strategie = _politiques.Resolve(politique);
        if (strategie is null)
            return OrderResult.Echec(RaisonEchec.EntreeInvalide,
                $"Politique tarifaire inconnue : '{politique}'.");

        var commande = new Order(numeroTable, plats, _horloge.GetLocalNow().DateTime);
        commande.ApplyPricing(strategie);

        _depot.Add(commande);

        var message = commande.EnterInitialState();
        _publieur.Notify(new OrderNotification(commande, commande.Status, message));

        return OrderResult.Ok(commande, message);
    }

    public OrderResult FaireProgresser(string identifiant)
    {
        var commande = _depot.GetById(identifiant);
        if (commande is null)
            return OrderResult.Echec(RaisonEchec.Introuvable,
                $"Commande '{identifiant}' introuvable.");

        var transition = commande.TryAdvance();
        if (!transition.Reussi)
            return OrderResult.Echec(RaisonEchec.TransitionRefusee, transition.Message);

        _publieur.Notify(new OrderNotification(commande, transition.Statut, transition.Message));

        return OrderResult.Ok(commande, transition.Message);
    }
}
