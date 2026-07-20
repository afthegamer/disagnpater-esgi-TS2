namespace RestaurantApi.Domain.Menu;

public interface IMenuItemFactoryProvider
{
    MenuItem Create(MenuCategory categorie, string id, string name, decimal price, int preparationTimeMinutes);

    IReadOnlyCollection<MenuCategory> CategoriesDisponibles { get; }
}

public sealed class MenuItemFactoryProvider : IMenuItemFactoryProvider
{
    private readonly Dictionary<MenuCategory, IMenuItemFactory> _fabriques;

    public MenuItemFactoryProvider(IEnumerable<IMenuItemFactory> fabriques)
        => _fabriques = fabriques.ToDictionary(f => f.Category);

    public IReadOnlyCollection<MenuCategory> CategoriesDisponibles => _fabriques.Keys;

    public MenuItem Create(MenuCategory categorie, string id, string name, decimal price, int preparationTimeMinutes)
    {
        if (!_fabriques.TryGetValue(categorie, out var fabrique))
            throw new ArgumentOutOfRangeException(
                nameof(categorie), categorie, "Aucune fabrique enregistrée pour cette catégorie.");

        return fabrique.Create(id, name, price, preparationTimeMinutes);
    }
}
