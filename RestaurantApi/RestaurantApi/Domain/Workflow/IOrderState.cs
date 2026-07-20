using RestaurantApi.Domain.Orders;

namespace RestaurantApi.Domain.Workflow;

public interface IOrderState
{
    OrderStatus Status { get; }

    bool IsFinal { get; }

    string OnEnter(Order order);

    IOrderState? Next();
}
