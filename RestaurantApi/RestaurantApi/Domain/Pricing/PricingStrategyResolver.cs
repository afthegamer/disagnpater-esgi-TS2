namespace RestaurantApi.Domain.Pricing;

public interface IPricingStrategyResolver
{
    IPricingStrategy? Resolve(string? nom);

    IReadOnlyCollection<IPricingStrategy> All { get; }
}

public sealed class PricingStrategyResolver : IPricingStrategyResolver
{
    private readonly Dictionary<string, IPricingStrategy> _parNom;
    private readonly IPricingStrategy _defaut;

    public PricingStrategyResolver(IEnumerable<IPricingStrategy> strategies)
    {
        _parNom = strategies.ToDictionary(s => s.Name, StringComparer.OrdinalIgnoreCase);
        _defaut = _parNom[StandardPricing.Nom];
    }

    public IReadOnlyCollection<IPricingStrategy> All => _parNom.Values;

    public IPricingStrategy? Resolve(string? nom)
    {
        if (string.IsNullOrWhiteSpace(nom))
            return _defaut;

        return _parNom.TryGetValue(nom.Trim(), out var strategie) ? strategie : null;
    }
}
