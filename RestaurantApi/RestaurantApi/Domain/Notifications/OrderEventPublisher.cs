namespace RestaurantApi.Domain.Notifications;

public sealed class OrderEventPublisher : IOrderSubject
{
    private readonly ILogger<OrderEventPublisher> _logger;
    private readonly List<IOrderObserver> _observateurs = [];
    private readonly object _verrou = new();

    public OrderEventPublisher(ILogger<OrderEventPublisher> logger) => _logger = logger;

    public IReadOnlyCollection<string> Abonnes
    {
        get { lock (_verrou) { return _observateurs.Select(o => o.ServiceName).ToArray(); } }
    }

    public void Attach(IOrderObserver observer)
    {
        lock (_verrou)
        {
            if (!_observateurs.Contains(observer))
                _observateurs.Add(observer);
        }
    }

    public void Detach(IOrderObserver observer)
    {
        lock (_verrou) { _observateurs.Remove(observer); }
    }

    public void Notify(OrderNotification notification)
    {
        IOrderObserver[] instantane;
        lock (_verrou) { instantane = _observateurs.ToArray(); }

        foreach (var observateur in instantane)
        {
            try
            {
                observateur.OnOrderEvent(notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Le service {Service} a echoue sur la notification {Statut}.",
                    observateur.ServiceName, notification.Status);
            }
        }
    }
}
