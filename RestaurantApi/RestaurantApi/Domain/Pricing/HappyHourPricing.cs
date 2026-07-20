using RestaurantApi.Domain.Menu;

namespace RestaurantApi.Domain.Pricing;

public sealed class HappyHourPricing : IPricingStrategy
{
    public const string Nom = "happy-hour";

    private static readonly TimeOnly Debut = new(15, 0);
    private static readonly TimeOnly Fin = new(19, 0);
    private const decimal Taux = 0.20m;

    private readonly TimeProvider _horloge;

    public HappyHourPricing(TimeProvider horloge) => _horloge = horloge;

    public string Name => Nom;
    public string Description => "Happy Hour : -20% entre 15h00 et 19h00.";

    public decimal CalculateTotal(IReadOnlyCollection<MenuItem> items)
    {
        var sousTotal = items.Sum(item => item.Price);
        var maintenant = TimeOnly.FromDateTime(_horloge.GetLocalNow().DateTime);

        var dansLaFenetre = maintenant >= Debut && maintenant < Fin;

        return dansLaFenetre
            ? decimal.Round(sousTotal * (1 - Taux), 2, MidpointRounding.AwayFromZero)
            : sousTotal;
    }
}
