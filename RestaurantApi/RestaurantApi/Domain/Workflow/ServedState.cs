using RestaurantApi.Domain.Orders;

namespace RestaurantApi.Domain.Workflow;

public sealed class ServedState : IOrderState
{
    public OrderStatus Status => OrderStatus.Served;
    public bool IsFinal => false;

    public string OnEnter(Order order)
        => $"Commande servie - table {order.TableNumber}. Reste a encaisser : {order.TotalPrice:0.00} EUR.";

    public IOrderState? Next() => new PaidState();
}
