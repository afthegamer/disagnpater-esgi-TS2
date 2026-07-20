using RestaurantApi.Domain.Menu;

namespace RestaurantApi.Domain.Pricing;

public interface IPricingStrategy
{
    string Name { get; }

    string Description { get; }

    decimal CalculateTotal(IReadOnlyCollection<MenuItem> items);
}
