using RestaurantApi.Domain.Workflow;

namespace RestaurantApi.Domain.Notifications;

public sealed class KitchenService : IOrderObserver
{
    private readonly ILogger<KitchenService> _logger;

    public KitchenService(ILogger<KitchenService> logger) => _logger = logger;

    public string ServiceName => "Cuisine";

    public void OnOrderEvent(OrderNotification notification)
    {
        switch (notification.Status)
        {
            case OrderStatus.Received:
                _logger.LogInformation("[CUISINE] {Message}", notification.Message);
                break;

            case OrderStatus.InPreparation:
                _logger.LogInformation("[CUISINE] {Message}", notification.Message);
                break;

            default:
                break;
        }
    }
}
