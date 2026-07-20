using RestaurantApi.Domain.Menu;

namespace RestaurantApi.Tests;

public class MenuItemFactoryTests
{
    private static MenuItemFactoryProvider Fabriques() => new([
        new StarterFactory(),
        new MainCourseFactory(),
        new DessertFactory(),
        new BeverageFactory()
    ]);

    [Theory]
    [InlineData(MenuCategory.Entree, typeof(Starter))]
    [InlineData(MenuCategory.Plat, typeof(MainCourse))]
    [InlineData(MenuCategory.Dessert, typeof(Dessert))]
    [InlineData(MenuCategory.Boisson, typeof(Beverage))]
    public void Chaque_categorie_produit_son_propre_type(MenuCategory categorie, Type typeAttendu)
    {
        var plat = Fabriques().Create(categorie, "id", "nom", 10m, 5);

        Assert.IsType(typeAttendu, plat);
        Assert.Equal(categorie, plat.Category);
    }

    [Fact]
    public void La_categorie_est_imposee_par_le_type_et_non_saisie()
    {
        var plat = Fabriques().Create(MenuCategory.Entree, "id", "nom", 10m, 5);

        Assert.Equal(MenuCategory.Entree, plat.Category);
    }

    [Fact]
    public void L_ordre_de_service_distingue_les_categories()
    {
        var fabriques = Fabriques();

        var boisson = fabriques.Create(MenuCategory.Boisson, "b", "eau", 3m, 1);
        var entree = fabriques.Create(MenuCategory.Entree, "e", "salade", 8m, 5);
        var dessert = fabriques.Create(MenuCategory.Dessert, "d", "tarte", 7m, 5);

        Assert.True(boisson.ServingOrder < entree.ServingOrder);
        Assert.True(entree.ServingOrder < dessert.ServingOrder);
    }

    [Fact]
    public void Le_fournisseur_expose_les_quatre_categories()
    {
        Assert.Equal(4, Fabriques().CategoriesDisponibles.Count);
    }
}
