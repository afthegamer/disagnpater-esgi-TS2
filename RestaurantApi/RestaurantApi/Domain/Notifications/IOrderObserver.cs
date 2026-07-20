using RestaurantApi.Domain.Orders;
using RestaurantApi.Domain.Workflow;

namespace RestaurantApi.Domain.Notifications;

public sealed record OrderNotification(Order Order, OrderStatus Status, string Message);

public interface IOrderObserver
{
    string ServiceName { get; }

    void OnOrderEvent(OrderNotification notification);
}

public interface IOrderSubject
{
    void Attach(IOrderObserver observer);
    void Detach(IOrderObserver observer);
    void Notify(OrderNotification notification);
}
