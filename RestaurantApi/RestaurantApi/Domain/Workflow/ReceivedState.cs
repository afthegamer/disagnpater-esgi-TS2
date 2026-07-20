using RestaurantApi.Domain.Orders;

namespace RestaurantApi.Domain.Workflow;

public sealed class ReceivedState : IOrderState
{
    public OrderStatus Status => OrderStatus.Received;
    public bool IsFinal => false;

    public string OnEnter(Order order)
        => $"Commande recue - table {order.TableNumber}, {order.Items.Count} plat(s), {order.TotalPrice:0.00} EUR.";

    public IOrderState? Next() => new InPreparationState();
}
