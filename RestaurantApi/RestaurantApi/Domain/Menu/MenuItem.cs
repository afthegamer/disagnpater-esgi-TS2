namespace RestaurantApi.Domain.Menu;

public abstract class MenuItem
{
    protected MenuItem(string id, string name, decimal price, int preparationTimeMinutes)
    {
        Id = id;
        Name = name;
        Price = price;
        PreparationTimeMinutes = preparationTimeMinutes;
    }

    public string Id { get; }
    public string Name { get; }
    public decimal Price { get; }
    public int PreparationTimeMinutes { get; }

    public abstract MenuCategory Category { get; }

    public abstract int ServingOrder { get; }
}
