using Microsoft.Extensions.Logging.Abstractions;
using RestaurantApi.Domain.Menu;
using RestaurantApi.Domain.Notifications;
using RestaurantApi.Domain.Orders;
using RestaurantApi.Domain.Pricing;
using RestaurantApi.Domain.Workflow;

namespace RestaurantApi.Tests;

public class OrderNotificationTests
{
    private sealed class ObservateurEspion : IOrderObserver
    {
        public ObservateurEspion(string nom) => ServiceName = nom;

        public string ServiceName { get; }
        public List<OrderStatus> Recus { get; } = [];

        public void OnOrderEvent(OrderNotification notification) => Recus.Add(notification.Status);
    }

    private sealed class ObservateurDefaillant : IOrderObserver
    {
        public string ServiceName => "Defaillant";

        public void OnOrderEvent(OrderNotification notification)
            => throw new InvalidOperationException("panne simulee");
    }

    private static OrderEventPublisher Publieur()
        => new(NullLogger<OrderEventPublisher>.Instance);

    private static OrderNotification UneNotification(OrderStatus statut)
    {
        var commande = new Order(1, [new MainCourse("p", "Steak", 18m, 20)], DateTime.MinValue);
        commande.ApplyPricing(new StandardPricing());
        return new OrderNotification(commande, statut, "message");
    }

    [Fact]
    public void Tous_les_abonnes_recoivent_l_evenement()
    {
        var publieur = Publieur();
        var cuisine = new ObservateurEspion("Cuisine");
        var salle = new ObservateurEspion("Salle");

        publieur.Attach(cuisine);
        publieur.Attach(salle);
        publieur.Notify(UneNotification(OrderStatus.Ready));

        Assert.Equal([OrderStatus.Ready], cuisine.Recus);
        Assert.Equal([OrderStatus.Ready], salle.Recus);
    }

    [Fact]
    public void Un_service_desabonne_ne_recoit_plus_rien()
    {
        var publieur = Publieur();
        var espion = new ObservateurEspion("Cuisine");

        publieur.Attach(espion);
        publieur.Notify(UneNotification(OrderStatus.Received));
        publieur.Detach(espion);
        publieur.Notify(UneNotification(OrderStatus.Ready));

        Assert.Equal([OrderStatus.Received], espion.Recus);
    }

    [Fact]
    public void Un_abonnement_en_double_ne_produit_pas_de_doublon()
    {
        var publieur = Publieur();
        var espion = new ObservateurEspion("Cuisine");

        publieur.Attach(espion);
        publieur.Attach(espion);
        publieur.Notify(UneNotification(OrderStatus.Received));

        Assert.Single(espion.Recus);
    }

    [Fact]
    public void Un_observateur_en_panne_n_empeche_pas_les_autres_d_etre_prevenus()
    {
        var publieur = Publieur();
        var survivant = new ObservateurEspion("Salle");

        publieur.Attach(new ObservateurDefaillant());
        publieur.Attach(survivant);

        publieur.Notify(UneNotification(OrderStatus.Ready)); // ne doit pas lever

        Assert.Single(survivant.Recus);
    }

    [Fact]
    public void Chaque_service_filtre_ce_qui_le_concerne()
    {
        var publieur = Publieur();
        publieur.Attach(new KitchenService(NullLogger<KitchenService>.Instance));
        publieur.Attach(new DiningRoomService(NullLogger<DiningRoomService>.Instance));
        publieur.Attach(new BillingService(NullLogger<BillingService>.Instance));

        foreach (var statut in Enum.GetValues<OrderStatus>())
            publieur.Notify(UneNotification(statut));

        Assert.Equal(3, publieur.Abonnes.Count);
    }
}
