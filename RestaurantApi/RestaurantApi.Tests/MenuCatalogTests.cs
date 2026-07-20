using RestaurantApi.Domain.Menu;

namespace RestaurantApi.Tests;

public class MenuCatalogTests
{
    private static MenuItem UnPlat(string id) => new MainCourse(id, "Plat " + id, 15m, 10);

    [Fact]
    public void Un_plat_ajoute_est_retrouvable()
    {
        var catalogue = new MenuCatalog();

        Assert.True(catalogue.Ajouter(UnPlat("tartare")));
        Assert.NotNull(catalogue.Trouver("tartare"));
        Assert.True(catalogue.Contient("TARTARE"));
    }

    [Fact]
    public void Un_identifiant_deja_pris_est_refuse_sans_ecraser_l_existant()
    {
        var catalogue = new MenuCatalog();
        catalogue.Ajouter(new MainCourse("steak", "Steak", 18m, 20));

        var refuse = catalogue.Ajouter(new Dessert("steak", "Imposteur", 3m, 1));

        Assert.False(refuse);
        Assert.Equal("Steak", catalogue.Trouver("steak")!.Name);
    }

    [Fact]
    public void Un_identifiant_absent_ou_vide_ne_leve_pas()
    {
        var catalogue = new MenuCatalog();

        Assert.Null(catalogue.Trouver("inexistant"));
        Assert.Null(catalogue.Trouver(""));
        Assert.Null(catalogue.Trouver("   "));
    }

    [Fact]
    public void Chaque_lecture_renvoie_un_instantane_distinct()
    {
        var catalogue = new MenuCatalog();
        catalogue.Ajouter(UnPlat("a"));

        var premiere = catalogue.Plats;
        catalogue.Ajouter(UnPlat("b"));
        var seconde = catalogue.Plats;

        Assert.Single(premiere);
        Assert.Equal(2, seconde.Count);
    }

    [Fact]
    public async Task Des_ajouts_concurrents_sont_tous_conserves_sans_doublon()
    {
        var catalogue = new MenuCatalog();
        const int concurrents = 64;

        var depart = new ManualResetEventSlim(false);
        var taches = Enumerable.Range(0, concurrents).Select(i => Task.Run(() =>
        {
            depart.Wait();
            catalogue.Ajouter(UnPlat("plat-" + i % 32));
        })).ToArray();

        depart.Set();
        await Task.WhenAll(taches);

        Assert.Equal(32, catalogue.Plats.Count);
        Assert.Equal(32, catalogue.Plats.Select(p => p.Id).Distinct().Count());
    }
}
