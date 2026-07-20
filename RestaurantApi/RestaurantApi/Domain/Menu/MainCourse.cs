namespace RestaurantApi.Domain.Menu;

public sealed class MainCourse : MenuItem
{
    public MainCourse(string id, string name, decimal price, int preparationTimeMinutes)
        : base(id, name, price, preparationTimeMinutes) { }

    public override MenuCategory Category => MenuCategory.Plat;
    public override int ServingOrder => 3;
}
