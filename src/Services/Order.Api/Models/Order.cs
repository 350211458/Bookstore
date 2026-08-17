namespace Order.Api.Models;

/// <summary>
/// A placed order (spec 04). <see cref="TotalAmount"/> is the sum of the line totals of
/// its OrderItems. Field list is authoritative per the spec.
/// </summary>
public sealed class Order
{
    public int Id { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public OrderStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
