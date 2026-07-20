using RestaurantApi.Domain.Orders;

namespace RestaurantApi.Domain.Workflow;

public sealed class PaidState : IOrderState
{
    public OrderStatus Status => OrderStatus.Paid;
    public bool IsFinal => true;

    public string OnEnter(Order order)
        => $"Commande reglee - table {order.TableNumber}, {order.TotalPrice:0.00} EUR encaisses. Dossier clos.";

    public IOrderState? Next() => null;
}
