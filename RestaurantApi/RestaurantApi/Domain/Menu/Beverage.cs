namespace RestaurantApi.Domain.Menu;

public sealed class Beverage : MenuItem
{
    public Beverage(string id, string name, decimal price, int preparationTimeMinutes)
        : base(id, name, price, preparationTimeMinutes) { }

    public override MenuCategory Category => MenuCategory.Boisson;
    public override int ServingOrder => 1;
}
