using RestaurantApi.Domain.Menu;

namespace RestaurantApi.Domain.Pricing;

public sealed class MenuFormulaPricing : IPricingStrategy
{
    public const string Nom = "formule";

    private const decimal PrixFixe = 25m;

    public string Name => Nom;
    public string Description => "Formule menu : 25 EUR si entree + plat + dessert.";

    public decimal CalculateTotal(IReadOnlyCollection<MenuItem> items)
    {
        bool Contient(MenuCategory categorie) => items.Any(item => item.Category == categorie);

        var eligible = Contient(MenuCategory.Entree)
                       && Contient(MenuCategory.Plat)
                       && Contient(MenuCategory.Dessert);

        var sousTotal = items.Sum(item => item.Price);

        return eligible ? Math.Min(PrixFixe, sousTotal) : sousTotal;
    }
}
