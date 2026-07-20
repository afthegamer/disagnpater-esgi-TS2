using RestaurantApi.Domain.Menu;

namespace RestaurantApi.Domain.Pricing;

public sealed class StandardPricing : IPricingStrategy
{
    public const string Nom = "standard";

    public string Name => Nom;
    public string Description => "Tarif standard : somme des prix de base.";

    public decimal CalculateTotal(IReadOnlyCollection<MenuItem> items)
        => items.Sum(item => item.Price);
}
