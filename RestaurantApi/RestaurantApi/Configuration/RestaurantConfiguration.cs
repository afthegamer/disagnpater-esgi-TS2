using RestaurantApi.Domain.Menu;

namespace RestaurantApi.Configuration;

public sealed class RestaurantConfiguration
{
    private static readonly object Verrou = new();
    private static RestaurantConfiguration? _instance;

    private readonly MenuCatalog _catalogue;

    private RestaurantConfiguration(MenuCatalog catalogue, OpeningHours horaires)
    {
        _catalogue = catalogue;
        Horaires = horaires;
    }

    public static RestaurantConfiguration Instance =>
        _instance ?? throw new InvalidOperationException(
            "Configuration non initialisee : appeler Initialize() au demarrage.");

    public static RestaurantConfiguration Initialize(
        IMenuItemFactoryProvider fabriques,
        MenuCatalog catalogue)
    {
        lock (Verrou)
        {
            if (_instance is not null) return _instance;

            foreach (var plat in ConstruireCarteInitiale(fabriques))
                catalogue.Ajouter(plat);

            return _instance = new RestaurantConfiguration(catalogue, OpeningHours.ParDefaut());
        }
    }

    public IReadOnlyList<MenuItem> Menu => _catalogue.Plats;

    public OpeningHours Horaires { get; }

    public string NomEtablissement => "Le Pattern Gourmand";
    public string Devise => "EUR";

    public MenuItem? TrouverPlat(string id) => _catalogue.Trouver(id);

    private static IEnumerable<MenuItem> ConstruireCarteInitiale(IMenuItemFactoryProvider fabriques) =>
    [
        fabriques.Create(MenuCategory.Entree,  "salade-cesar",     "Salade Cesar",        8.50m,  5),
        fabriques.Create(MenuCategory.Entree,  "soupe-du-jour",    "Soupe du jour",       6.00m,  8),
        fabriques.Create(MenuCategory.Plat,    "steak-frites",     "Steak frites",       18.50m, 20),
        fabriques.Create(MenuCategory.Plat,    "saumon-grille",    "Saumon grille",      21.00m, 18),
        fabriques.Create(MenuCategory.Plat,    "risotto",          "Risotto aux cepes",  16.00m, 25),
        fabriques.Create(MenuCategory.Dessert, "tarte-tatin",      "Tarte Tatin",         7.50m,  6),
        fabriques.Create(MenuCategory.Dessert, "mousse-chocolat",  "Mousse au chocolat",  6.50m,  4),
        fabriques.Create(MenuCategory.Boisson, "eau-petillante",   "Eau petillante",      3.50m,  1),
        fabriques.Create(MenuCategory.Boisson, "verre-vin-rouge",  "Verre de vin rouge",  5.50m,  1),
        fabriques.Create(MenuCategory.Boisson, "cafe",             "Cafe",                2.50m,  2)
    ];
}
