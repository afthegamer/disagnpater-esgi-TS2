using Microsoft.Extensions.Logging.Abstractions;
using RestaurantApi.Configuration;
using RestaurantApi.Domain.Menu;
using RestaurantApi.Domain.Notifications;
using RestaurantApi.Domain.Orders;
using RestaurantApi.Domain.Pricing;
using RestaurantApi.Domain.Workflow;
using RestaurantApi.Repositories;

namespace RestaurantApi.Tests;

public class OrderServiceTests
{
    private sealed class ObservateurEspion : IOrderObserver
    {
        public string ServiceName => "Espion";
        public List<OrderNotification> Recus { get; } = [];
        public void OnOrderEvent(OrderNotification notification) => Recus.Add(notification);
    }

    private static (OrderService service, ObservateurEspion espion) Monter(int heure = 12)
    {
        var fabriques = new MenuItemFactoryProvider([
            new StarterFactory(), new MainCourseFactory(),
            new DessertFactory(), new BeverageFactory()
        ]);

        var horloge = new HorlogeFigee(heure);
        var configuration = RestaurantConfiguration.Initialize(fabriques, new MenuCatalog());

        var publieur = new OrderEventPublisher(NullLogger<OrderEventPublisher>.Instance);
        var espion = new ObservateurEspion();
        publieur.Attach(espion);

        var resolver = new PricingStrategyResolver([
            new StandardPricing(),
            new HappyHourPricing(horloge),
            new GroupDiscountPricing(),
            new MenuFormulaPricing()
        ]);

        return (new OrderService(new OrderRepository(), resolver, publieur, configuration, horloge), espion);
    }

    [Fact]
    public void La_politique_demandee_est_reellement_appliquee_au_prix()
    {
        var (service, _) = Monter();

        var resultat = service.Creer(4, ["salade-cesar", "steak-frites", "tarte-tatin"], "formule");

        Assert.True(resultat.Reussi);
        Assert.Equal(34.50m, resultat.Order!.Subtotal);
        Assert.Equal(25.00m, resultat.Order.TotalPrice);
        Assert.Equal("formule", resultat.Order.PricingPolicy);
        Assert.True(resultat.Order.DiscountApplied);
    }

    [Fact]
    public void Happy_hour_s_applique_dans_la_fenetre()
    {
        var (service, _) = Monter(heure: 17);

        var resultat = service.Creer(1, ["salade-cesar", "steak-frites"], "happy-hour");

        Assert.Equal(27.00m, resultat.Order!.Subtotal);
        Assert.Equal(21.60m, resultat.Order.TotalPrice);
        Assert.True(resultat.Order.DiscountApplied);
    }

    [Fact]
    public void Une_politique_sans_effet_est_tracee_comme_telle()
    {
        var (service, _) = Monter(heure: 12);

        var resultat = service.Creer(1, ["salade-cesar", "steak-frites"], "happy-hour");

        Assert.Equal(27.00m, resultat.Order!.TotalPrice);
        Assert.Equal(resultat.Order.Subtotal, resultat.Order.TotalPrice);
        Assert.False(resultat.Order.DiscountApplied);
    }

    [Fact]
    public void Sans_politique_precisee_le_tarif_standard_s_applique()
    {
        var (service, _) = Monter();

        var resultat = service.Creer(2, ["cafe"], null);

        Assert.Equal(2.50m, resultat.Order!.TotalPrice);
        Assert.Equal("standard", resultat.Order.PricingPolicy);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Un_numero_de_table_non_positif_est_refuse(int table)
    {
        var (service, _) = Monter();

        var resultat = service.Creer(table, ["cafe"], null);

        Assert.False(resultat.Reussi);
        Assert.Equal(RaisonEchec.EntreeInvalide, resultat.Raison);
    }

    [Fact]
    public void Une_commande_vide_est_refusee()
    {
        var (service, _) = Monter();

        Assert.False(service.Creer(1, [], null).Reussi);
    }

    [Fact]
    public void Un_plat_hors_menu_est_refuse()
    {
        var (service, _) = Monter();

        var resultat = service.Creer(1, ["caviar"], null);

        Assert.False(resultat.Reussi);
        Assert.Contains("caviar", resultat.Erreur);
    }

    [Fact]
    public void Une_politique_inconnue_est_refusee()
    {
        var (service, _) = Monter();

        var resultat = service.Creer(1, ["cafe"], "tarif-du-patron");

        Assert.False(resultat.Reussi);
        Assert.Equal(RaisonEchec.EntreeInvalide, resultat.Raison);
    }

    [Fact]
    public void Une_commande_demesuree_est_refusee()
    {
        var (service, _) = Monter();
        var trop = Enumerable.Repeat("cafe", 500).ToArray();

        Assert.False(service.Creer(1, trop, null).Reussi);
    }

    [Fact]
    public void Le_workflow_progresse_puis_se_ferme()
    {
        var (service, _) = Monter();
        var id = service.Creer(3, ["cafe"], null).Order!.Id;

        Assert.Equal(OrderStatus.InPreparation, service.FaireProgresser(id).Order!.Status);
        Assert.Equal(OrderStatus.Ready, service.FaireProgresser(id).Order!.Status);
        Assert.Equal(OrderStatus.Served, service.FaireProgresser(id).Order!.Status);
        Assert.Equal(OrderStatus.Paid, service.FaireProgresser(id).Order!.Status);

        var refus = service.FaireProgresser(id);
        Assert.False(refus.Reussi);
        Assert.Equal(RaisonEchec.TransitionRefusee, refus.Raison);
    }

    [Fact]
    public void Une_commande_inconnue_et_une_transition_refusee_ont_des_motifs_distincts()
    {
        var (service, _) = Monter();

        Assert.Equal(RaisonEchec.Introuvable, service.FaireProgresser("inexistant").Raison);
    }

    [Fact]
    public void Chaque_etape_est_publiee_aux_observateurs()
    {
        var (service, espion) = Monter();
        var id = service.Creer(3, ["cafe"], null).Order!.Id;

        for (var i = 0; i < 4; i++) service.FaireProgresser(id);

        Assert.Equal(
            [OrderStatus.Received, OrderStatus.InPreparation, OrderStatus.Ready,
             OrderStatus.Served, OrderStatus.Paid],
            espion.Recus.Select(n => n.Status));
    }

    [Fact]
    public void Le_message_de_l_etape_accompagne_la_notification_et_le_resultat()
    {
        var (service, espion) = Monter();
        var id = service.Creer(3, ["steak-frites"], null).Order!.Id;

        var resultat = service.FaireProgresser(id);

        Assert.Contains("~20 min", resultat.Message);
        Assert.Contains("~20 min", espion.Recus.Last().Message);
    }

    [Fact]
    public void Une_transition_refusee_ne_publie_rien()
    {
        var (service, espion) = Monter();
        var id = service.Creer(3, ["cafe"], null).Order!.Id;
        for (var i = 0; i < 4; i++) service.FaireProgresser(id);
        var avant = espion.Recus.Count;

        service.FaireProgresser(id);

        Assert.Equal(avant, espion.Recus.Count);
    }
}
