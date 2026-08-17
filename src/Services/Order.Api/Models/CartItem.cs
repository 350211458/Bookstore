namespace Order.Api.Models;

/// <summary>
/// A shopping-cart line, keyed by the anonymous <see cref="SessionId"/> (spec 04).
/// Field list is authoritative per the spec.
/// </summary>
public sealed class CartItem
{
    public int Id { get; set; }

    public string SessionId { get; set; } = string.Empty;

    public int BookId { get; set; }

    public string Title { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }
}
