namespace RestaurantApi.Domain.Menu;

public interface IMenuItemFactory
{
    MenuCategory Category { get; }

    MenuItem Create(string id, string name, decimal price, int preparationTimeMinutes);
}

public sealed class StarterFactory : IMenuItemFactory
{
    public MenuCategory Category => MenuCategory.Entree;

    public MenuItem Create(string id, string name, decimal price, int preparationTimeMinutes)
        => new Starter(id, name, price, preparationTimeMinutes);
}

public sealed class MainCourseFactory : IMenuItemFactory
{
    public MenuCategory Category => MenuCategory.Plat;

    public MenuItem Create(string id, string name, decimal price, int preparationTimeMinutes)
        => new MainCourse(id, name, price, preparationTimeMinutes);
}

public sealed class DessertFactory : IMenuItemFactory
{
    public MenuCategory Category => MenuCategory.Dessert;

    public MenuItem Create(string id, string name, decimal price, int preparationTimeMinutes)
        => new Dessert(id, name, price, preparationTimeMinutes);
}

public sealed class BeverageFactory : IMenuItemFactory
{
    public MenuCategory Category => MenuCategory.Boisson;

    public MenuItem Create(string id, string name, decimal price, int preparationTimeMinutes)
        => new Beverage(id, name, price, preparationTimeMinutes);
}
