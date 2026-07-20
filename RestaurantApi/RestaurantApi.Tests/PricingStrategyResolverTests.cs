using RestaurantApi.Domain.Pricing;

namespace RestaurantApi.Tests;

public class PricingStrategyResolverTests
{
    private static PricingStrategyResolver Resolver() => new([
        new StandardPricing(),
        new HappyHourPricing(new HorlogeFigee(17)),
        new GroupDiscountPricing(),
        new MenuFormulaPricing()
    ]);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Sans_politique_precisee_on_retombe_sur_le_standard(string? nom)
    {
        var strategie = Resolver().Resolve(nom);

        Assert.IsType<StandardPricing>(strategie);
    }

    [Fact]
    public void Resout_une_politique_par_son_nom()
    {
        var strategie = Resolver().Resolve("happy-hour");

        Assert.IsType<HappyHourPricing>(strategie);
    }

    [Fact]
    public void La_resolution_ignore_la_casse()
    {
        var strategie = Resolver().Resolve("HAPPY-HOUR");

        Assert.IsType<HappyHourPricing>(strategie);
    }

    [Fact]
    public void Un_nom_inconnu_retourne_null_et_non_une_exception()
    {
        var strategie = Resolver().Resolve("tarif-du-patron");

        Assert.Null(strategie);
    }

    [Fact]
    public void Le_catalogue_expose_toutes_les_politiques_enregistrees()
    {
        Assert.Equal(4, Resolver().All.Count);
    }
}
