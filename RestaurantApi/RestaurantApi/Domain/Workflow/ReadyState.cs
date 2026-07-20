using RestaurantApi.Domain.Orders;

namespace RestaurantApi.Domain.Workflow;

public sealed class ReadyState : IOrderState
{
    public OrderStatus Status => OrderStatus.Ready;
    public bool IsFinal => false;

    public string OnEnter(Order order)
    {
        var sequence = string.Join(", ", order.Items
            .OrderBy(item => item.ServingOrder)
            .Select(item => item.Name));

        return $"Commande prete - table {order.TableNumber}. Ordre de service : {sequence}.";
    }

    public IOrderState? Next() => new ServedState();
}
