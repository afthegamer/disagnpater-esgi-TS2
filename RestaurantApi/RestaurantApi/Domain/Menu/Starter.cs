namespace RestaurantApi.Domain.Menu;

public sealed class Starter : MenuItem
{
    public Starter(string id, string name, decimal price, int preparationTimeMinutes)
        : base(id, name, price, preparationTimeMinutes) { }

    public override MenuCategory Category => MenuCategory.Entree;
    public override int ServingOrder => 2;
}
