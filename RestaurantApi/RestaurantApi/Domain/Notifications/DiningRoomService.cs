using RestaurantApi.Domain.Workflow;

namespace RestaurantApi.Domain.Notifications;

public sealed class DiningRoomService : IOrderObserver
{
    private readonly ILogger<DiningRoomService> _logger;

    public DiningRoomService(ILogger<DiningRoomService> logger) => _logger = logger;

    public string ServiceName => "Salle";

    public void OnOrderEvent(OrderNotification notification)
    {
        switch (notification.Status)
        {
            case OrderStatus.Ready:
                _logger.LogInformation("[SALLE] {Message}", notification.Message);
                break;

            case OrderStatus.Served:
                _logger.LogInformation("[SALLE] {Message}", notification.Message);
                break;

            default:
                break;
        }
    }
}
