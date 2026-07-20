using RestaurantApi.Domain.Menu;
using RestaurantApi.Domain.Orders;
using RestaurantApi.Domain.Workflow;

namespace RestaurantApi.Api;

public sealed record CreateOrderRequest(
    int TableNumber,
    List<string> MenuItemIds,
    string? PricingPolicy);

public sealed record CreateMenuItemRequest(
    MenuCategory Category,
    string Id,
    string Name,
    decimal Price,
    int PreparationTimeMinutes);

public sealed record MenuItemResponse(
    string Id,
    string Name,
    decimal Price,
    MenuCategory Category,
    int PreparationTimeMinutes,
    int ServingOrder)
{
    public static MenuItemResponse De(MenuItem plat) => new(
        plat.Id, plat.Name, plat.Price, plat.Category, plat.PreparationTimeMinutes, plat.ServingOrder);
}

public sealed record OpeningHoursResponse(TimeOnly Opens, TimeOnly Closes, bool OpenNow);

public sealed record MenuResponse(
    string Restaurant,
    string Currency,
    OpeningHoursResponse OpeningHours,
    IReadOnlyList<MenuItemResponse> Items);

public sealed record OrderResponse(
    string Id,
    int TableNumber,
    IReadOnlyList<MenuItemResponse> Items,
    decimal Subtotal,
    decimal TotalPrice,
    string PricingPolicy,
    bool DiscountApplied,
    OrderStatus Status,
    bool CanAdvance,
    DateTime CreatedAt,
    string? Message)
{
    public static OrderResponse De(Order commande, string? message = null) => new(
        commande.Id,
        commande.TableNumber,
        commande.Items.Select(MenuItemResponse.De).ToArray(),
        commande.Subtotal,
        commande.TotalPrice,
        commande.PricingPolicy,
        commande.DiscountApplied,
        commande.Status,
        commande.CanAdvance,
        commande.CreatedAt,
        message);
}

public sealed record OrderStateResponse(
    string Id,
    OrderStatus Status,
    bool CanAdvance,
    string Message);

public sealed record PricingPolicyResponse(string Name, string Description);

public sealed record ErrorResponse(string Message, IReadOnlyList<string>? Accepted = null);
