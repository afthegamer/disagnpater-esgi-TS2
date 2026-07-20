using RestaurantApi.Domain.Menu;
using RestaurantApi.Domain.Orders;
using RestaurantApi.Domain.Pricing;
using RestaurantApi.Domain.Workflow;

namespace RestaurantApi.Tests;

public class OrderWorkflowTests
{
    private static Order UneCommande()
    {
        var commande = new Order(4, [
            new Starter("e", "Salade", 8m, 5),
            new MainCourse("p", "Steak", 18m, 20),
            new Dessert("d", "Tarte", 7m, 6)
        ], new DateTime(2026, 1, 1, 12, 0, 0));

        commande.ApplyPricing(new StandardPricing());
        return commande;
    }

    [Fact]
    public void Une_commande_nait_a_l_etat_recue()
    {
        Assert.Equal(OrderStatus.Received, UneCommande().Status);
    }

    [Fact]
    public void Les_etapes_s_enchainent_dans_l_ordre()
    {
        var commande = UneCommande();

        Assert.True(commande.TryAdvance().Reussi);
        Assert.Equal(OrderStatus.InPreparation, commande.Status);

        Assert.True(commande.TryAdvance().Reussi);
        Assert.Equal(OrderStatus.Ready, commande.Status);

        Assert.True(commande.TryAdvance().Reussi);
        Assert.Equal(OrderStatus.Served, commande.Status);

        Assert.True(commande.TryAdvance().Reussi);
        Assert.Equal(OrderStatus.Paid, commande.Status);
    }

    [Fact]
    public void Une_commande_payee_refuse_toute_transition_sans_lever_d_exception()
    {
        var commande = UneCommande();
        for (var i = 0; i < 4; i++) commande.TryAdvance();

        Assert.Equal(OrderStatus.Paid, commande.Status);
        Assert.False(commande.CanAdvance);

        var refus = commande.TryAdvance();

        Assert.False(refus.Reussi);
        Assert.Equal(OrderStatus.Paid, refus.Statut);
        Assert.Contains("termine", refus.Message);
    }

    [Fact]
    public void Chaque_etape_produit_son_propre_message()
    {
        var commande = UneCommande();

        var enPreparation = commande.TryAdvance();
        var prete = commande.TryAdvance();

        Assert.Contains("~20 min", enPreparation.Message);

        Assert.Contains("Salade, Steak, Tarte", prete.Message);
    }

    [Fact]
    public async Task Deux_progressions_concurrentes_ne_font_avancer_la_commande_qu_une_fois()
    {
        var commande = UneCommande();
        const int concurrents = 32;

        var depart = new ManualResetEventSlim(false);
        var resultats = new TransitionResult[concurrents];

        var taches = Enumerable.Range(0, concurrents).Select(i => Task.Run(() =>
        {
            depart.Wait();
            resultats[i] = commande.TryAdvance();
        })).ToArray();

        depart.Set();
        await Task.WhenAll(taches);

        Assert.Equal(4, resultats.Count(r => r.Reussi));
        Assert.Equal(concurrents - 4, resultats.Count(r => !r.Reussi));
        Assert.Equal(OrderStatus.Paid, commande.Status);
    }
}
