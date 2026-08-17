using Order.Api.Models;

namespace Order.Api.Services;

/// <summary>
/// Allowed <see cref="OrderStatus"/> transitions (spec 04). Terminals (Completed,
/// Cancelled) have no outgoing edges.
/// </summary>
public static class OrderStateMachine
{
    private static readonly Dictionary<OrderStatus, HashSet<OrderStatus>> Transitions = new()
    {
        [OrderStatus.Placed] = [OrderStatus.Paid, OrderStatus.Cancelled],
        [OrderStatus.Paid] = [OrderStatus.Processing, OrderStatus.Cancelled],
        [OrderStatus.Processing] = [OrderStatus.Shipped],
        [OrderStatus.Shipped] = [OrderStatus.Completed],
        [OrderStatus.Completed] = [],
        [OrderStatus.Cancelled] = [],
    };

    public static bool CanTransition(OrderStatus from, OrderStatus to) =>
        Transitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
}
