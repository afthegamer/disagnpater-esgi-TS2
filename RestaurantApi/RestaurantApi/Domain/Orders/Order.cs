using RestaurantApi.Domain.Menu;
using RestaurantApi.Domain.Pricing;
using RestaurantApi.Domain.Workflow;

namespace RestaurantApi.Domain.Orders;

public sealed record TransitionResult(bool Reussi, OrderStatus Statut, string Message);

public sealed class Order
{
    private readonly object _verrou = new();

    private IOrderState _state = new ReceivedState();

    public Order(int tableNumber, IReadOnlyList<MenuItem> items, DateTime createdAt)
    {
        TableNumber = tableNumber;
        Items = items;
        CreatedAt = createdAt;
    }

    public string Id { get; } = Guid.NewGuid().ToString();
    public int TableNumber { get; }
    public IReadOnlyList<MenuItem> Items { get; }

    public decimal Subtotal { get; private set; }

    public decimal TotalPrice { get; private set; }

    public string PricingPolicy { get; private set; } = "";

    public bool DiscountApplied => TotalPrice != Subtotal;

    public DateTime CreatedAt { get; }

    public OrderStatus Status
    {
        get { lock (_verrou) { return _state.Status; } }
    }

    public bool CanAdvance
    {
        get { lock (_verrou) { return !_state.IsFinal; } }
    }

    public void ApplyPricing(IPricingStrategy strategie)
    {
        Subtotal = Items.Sum(item => item.Price);
        TotalPrice = strategie.CalculateTotal(Items);
        PricingPolicy = strategie.Name;
    }

    public string EnterInitialState()
    {
        lock (_verrou) { return _state.OnEnter(this); }
    }

    public TransitionResult TryAdvance()
    {
        lock (_verrou)
        {
            var suivant = _state.Next();
            if (suivant is null)
                return new TransitionResult(false, _state.Status,
                    $"Commande deja {_state.Status} : le workflow est termine.");

            _state = suivant;
            return new TransitionResult(true, _state.Status, _state.OnEnter(this));
        }
    }
}
