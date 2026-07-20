using RestaurantApi.Domain.Orders;

namespace RestaurantApi.Domain.Workflow;

public sealed class InPreparationState : IOrderState
{
    public OrderStatus Status => OrderStatus.InPreparation;
    public bool IsFinal => false;

    public string OnEnter(Order order)
    {
        var attente = order.Items.Count == 0
            ? 0
            : order.Items.Max(item => item.PreparationTimeMinutes);

        return $"Preparation lancee - table {order.TableNumber}, prete dans ~{attente} min.";
    }

    public IOrderState? Next() => new ReadyState();
}
