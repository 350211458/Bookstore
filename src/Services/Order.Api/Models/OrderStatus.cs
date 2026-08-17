namespace Order.Api.Models;

/// <summary>Order lifecycle states (spec 04). Transitions are enforced by OrderStateMachine.</summary>
public enum OrderStatus
{
    Placed,
    Paid,
    Processing,
    Shipped,
    Completed,
    Cancelled,
}
