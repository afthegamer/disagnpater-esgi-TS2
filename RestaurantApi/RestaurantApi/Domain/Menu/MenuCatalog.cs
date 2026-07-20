using System.Collections.Concurrent;

namespace RestaurantApi.Domain.Menu;

public sealed class MenuCatalog
{
    private readonly ConcurrentDictionary<string, MenuItem> _plats = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<MenuItem> Plats => _plats.Values.ToArray();

    public MenuItem? Trouver(string id) =>
        string.IsNullOrWhiteSpace(id) ? null : _plats.GetValueOrDefault(id.Trim());

    public bool Contient(string id) => Trouver(id) is not null;

    public bool Ajouter(MenuItem plat) => _plats.TryAdd(plat.Id, plat);
}
