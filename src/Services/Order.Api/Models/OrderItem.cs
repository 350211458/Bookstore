namespace Order.Api.Models;

/// <summary>
/// A snapshot line of an <see cref="Order"/> (spec 04). Title/UnitPrice are copied from
/// the cart at checkout so later catalog changes do not affect the order.
/// </summary>
public sealed class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int BookId { get; set; }

    public string Title { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public decimal LineTotal { get; set; }
}
