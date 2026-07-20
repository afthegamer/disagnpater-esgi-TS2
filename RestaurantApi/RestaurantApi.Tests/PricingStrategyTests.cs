using RestaurantApi.Domain.Menu;
using RestaurantApi.Domain.Pricing;

namespace RestaurantApi.Tests;

public class PricingStrategyTests
{
    private static MenuItem UneEntree(decimal prix) => new Starter("e", "entree", prix, 5);
    private static MenuItem UnPlat(decimal prix) => new MainCourse("p", "plat", prix, 20);
    private static MenuItem UnDessert(decimal prix) => new Dessert("d", "dessert", prix, 5);
    private static MenuItem UneBoisson(decimal prix) => new Beverage("b", "boisson", prix, 1);

    [Fact]
    public void Standard_additionne_les_prix()
    {
        var total = new StandardPricing().CalculateTotal([UnPlat(10m), UnPlat(20m)]);

        Assert.Equal(30m, total);
    }

    [Theory]
    [InlineData(15, 0)]   // borne basse : incluse
    [InlineData(17, 0)]
    [InlineData(18, 59)]  // dernière minute
    public void HappyHour_applique_la_remise_dans_la_fenetre(int heure, int minute)
    {
        var strategie = new HappyHourPricing(new HorlogeFigee(heure, minute));

        var total = strategie.CalculateTotal([UnPlat(10m), UnPlat(20m)]);

        Assert.Equal(24m, total); // 30 - 20%
    }

    [Theory]
    [InlineData(14, 59)]  // juste avant
    [InlineData(19, 0)]   // borne haute : EXCLUE, choix assumé
    [InlineData(21, 30)]
    public void HappyHour_ne_fait_rien_hors_de_la_fenetre(int heure, int minute)
    {
        var strategie = new HappyHourPricing(new HorlogeFigee(heure, minute));

        var total = strategie.CalculateTotal([UnPlat(10m), UnPlat(20m)]);

        Assert.Equal(30m, total);
    }

    [Fact]
    public void Groupe_applique_la_remise_au_dela_du_seuil()
    {
        var total = new GroupDiscountPricing().CalculateTotal([UnPlat(60m)]);

        Assert.Equal(51m, total); // 60 - 15%
    }

    [Fact]
    public void Groupe_ne_fait_rien_a_50_euros_pile()
    {
        var total = new GroupDiscountPricing().CalculateTotal([UnPlat(50m)]);

        Assert.Equal(50m, total);
    }

    [Fact]
    public void Formule_applique_le_prix_fixe_quand_la_composition_est_complete()
    {
        var total = new MenuFormulaPricing()
            .CalculateTotal([UneEntree(8m), UnPlat(18m), UnDessert(7m)]);

        Assert.Equal(25m, total); // au lieu de 33
    }

    [Fact]
    public void Formule_tolere_un_plat_supplementaire()
    {
        var total = new MenuFormulaPricing()
            .CalculateTotal([UneEntree(8m), UnPlat(18m), UnDessert(7m), UneBoisson(4m)]);

        Assert.Equal(25m, total);
    }

    [Fact]
    public void Formule_retombe_sur_le_tarif_de_base_si_le_dessert_manque()
    {
        var total = new MenuFormulaPricing().CalculateTotal([UneEntree(8m), UnPlat(18m)]);

        Assert.Equal(26m, total);
    }
}
