using RestaurantApi.Domain.Menu;

namespace RestaurantApi.Domain.Pricing;

public sealed class GroupDiscountPricing : IPricingStrategy
{
    public const string Nom = "groupe";

    private const decimal Seuil = 50m;
    private const decimal Taux = 0.15m;

    public string Name => Nom;
    public string Description => "Reduction groupe : -15% au-dela de 50 EUR.";

    public decimal CalculateTotal(IReadOnlyCollection<MenuItem> items)
    {
        var sousTotal = items.Sum(item => item.Price);

        return sousTotal > Seuil
            ? decimal.Round(sousTotal * (1 - Taux), 2, MidpointRounding.AwayFromZero)
            : sousTotal;
    }
}
