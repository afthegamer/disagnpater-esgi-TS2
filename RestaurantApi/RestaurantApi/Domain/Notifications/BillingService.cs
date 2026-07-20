using RestaurantApi.Domain.Workflow;

namespace RestaurantApi.Domain.Notifications;

public sealed class BillingService : IOrderObserver
{
    private readonly ILogger<BillingService> _logger;

    public BillingService(ILogger<BillingService> logger) => _logger = logger;

    public string ServiceName => "Facturation";

    public void OnOrderEvent(OrderNotification notification)
    {
        var commande = notification.Order;

        switch (notification.Status)
        {
            case OrderStatus.Received:
                var effet = commande.DiscountApplied
                    ? $"{commande.PricingPolicy}, remise appliquee ({commande.Subtotal:0.00} -> {commande.TotalPrice:0.00})"
                    : $"{commande.PricingPolicy}, sans effet a cette heure - tarif de base applique";

                _logger.LogInformation(
                    "[FACTURATION] Commande {Id} ouverte - {Montant:0.00} EUR ({Effet}).",
                    commande.Id, commande.TotalPrice, effet);
                break;

            case OrderStatus.Paid:
                _logger.LogInformation(
                    "[FACTURATION] Commande {Id} reglee - {Montant:0.00} EUR encaisses.",
                    commande.Id, commande.TotalPrice);
                break;

            default:
                break;
        }
    }
}
