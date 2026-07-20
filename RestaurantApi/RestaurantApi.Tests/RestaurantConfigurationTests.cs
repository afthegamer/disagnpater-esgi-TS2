using RestaurantApi.Configuration;
using RestaurantApi.Domain.Menu;

namespace RestaurantApi.Tests;

public class RestaurantConfigurationTests
{
    private static MenuItemFactoryProvider Fabriques() => new([
        new StarterFactory(),
        new MainCourseFactory(),
        new DessertFactory(),
        new BeverageFactory()
    ]);

    private static RestaurantConfiguration Config() =>
        RestaurantConfiguration.Initialize(Fabriques(), new MenuCatalog());

    [Fact]
    public void Initialize_retourne_toujours_la_meme_instance()
    {
        var premiere = Config();
        var seconde = Config();

        Assert.True(ReferenceEquals(premiere, seconde));
    }

    [Fact]
    public void Le_point_d_acces_statique_expose_la_meme_instance()
    {
        var parInitialize = Config();

        Assert.True(ReferenceEquals(parInitialize, RestaurantConfiguration.Instance));
    }

    [Fact]
    public void Un_second_Initialize_ne_reconstruit_pas_la_carte()
    {
        var premiere = Config();
        var nbPlats = premiere.Menu.Count;

        var seconde = RestaurantConfiguration.Initialize(Fabriques(), new MenuCatalog());

        Assert.Same(premiere, seconde);
        Assert.Equal(nbPlats, seconde.Menu.Count);
    }

    [Fact]
    public void Le_menu_est_construit_par_les_fabriques()
    {
        var config = Config();

        Assert.NotEmpty(config.Menu);
        Assert.Contains(config.Menu, plat => plat is Starter);
        Assert.Contains(config.Menu, plat => plat is MainCourse);
        Assert.Contains(config.Menu, plat => plat is Dessert);
        Assert.Contains(config.Menu, plat => plat is Beverage);
    }

    [Fact]
    public void Chaque_lecture_du_menu_renvoie_un_instantane_independant()
    {
        var config = Config();

        var premiere = config.Menu;
        var seconde = config.Menu;

        Assert.NotSame(premiere, seconde);
        Assert.Equal(premiere.Count, seconde.Count);
    }

    [Fact]
    public void Un_plat_se_retrouve_par_son_identifiant_sans_tenir_compte_de_la_casse()
    {
        var config = Config();

        Assert.NotNull(config.TrouverPlat("steak-frites"));
        Assert.NotNull(config.TrouverPlat("STEAK-FRITES"));
        Assert.Null(config.TrouverPlat("caviar"));
    }

    [Fact]
    public void Les_horaires_encadrent_le_service()
    {
        var horaires = Config().Horaires;

        Assert.False(horaires.EstOuvert(new TimeOnly(9, 0)));
        Assert.True(horaires.EstOuvert(new TimeOnly(12, 30)));
        Assert.False(horaires.EstOuvert(new TimeOnly(23, 30)));
    }

    [Fact]
    public void Des_horaires_qui_franchissent_minuit_sont_gerees()
    {
        var nuit = new OpeningHours(new TimeOnly(18, 0), new TimeOnly(2, 0));

        Assert.True(nuit.EstOuvert(new TimeOnly(20, 0)));
        Assert.True(nuit.EstOuvert(new TimeOnly(1, 0)));
        Assert.False(nuit.EstOuvert(new TimeOnly(3, 0)));
        Assert.False(nuit.EstOuvert(new TimeOnly(12, 0)));
    }
}
