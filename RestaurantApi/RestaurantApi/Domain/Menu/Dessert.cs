namespace RestaurantApi.Domain.Menu;

public sealed class Dessert : MenuItem
{
    public Dessert(string id, string name, decimal price, int preparationTimeMinutes)
        : base(id, name, price, preparationTimeMinutes) { }

    public override MenuCategory Category => MenuCategory.Dessert;
    public override int ServingOrder => 4;
}
