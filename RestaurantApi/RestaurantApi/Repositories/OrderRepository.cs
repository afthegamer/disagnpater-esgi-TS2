using System.Collections.Concurrent;
using RestaurantApi.Domain.Orders;

namespace RestaurantApi.Repositories;

public class OrderRepository
{
    private readonly ConcurrentDictionary<string, Order> _orders = new();

    public void Add(Order order) => _orders[order.Id] = order;

    public Order? GetById(string id) => _orders.TryGetValue(id, out var order) ? order : null;

    public List<Order> GetAll() => _orders.Values.ToList();
}
