using System.ComponentModel.DataAnnotations;

namespace Order.Api.Models;

/// <summary>Request body for POST /api/cart/items (spec 04).</summary>
public sealed record AddCartItemRequest(
    [param: Required] string SessionId,
    int BookId,
    [param: Required] string Title,
    [param: Range(0, double.MaxValue)] decimal UnitPrice,
    [param: Range(1, int.MaxValue)] int Quantity);

/// <summary>Request body for PATCH /api/cart/items/{bookId} (spec 04).</summary>
public sealed record UpdateCartItemQuantityRequest([param: Range(1, int.MaxValue)] int Quantity);

/// <summary>Request body for POST /api/orders/checkout (spec 04).</summary>
public sealed record CheckoutRequest(
    [param: Required] string SessionId,
    [param: Required] string CustomerName);

/// <summary>Request body for POST /api/orders/{id}/status (spec 04).</summary>
public sealed record UpdateOrderStatusRequest(OrderStatus Status);

/// <summary>Response for GET /api/cart (spec 04): the lines plus the computed total.</summary>
public sealed record CartResponse(IReadOnlyList<CartItem> Items, decimal TotalAmount);

/// <summary>Response for an order with its items (spec 04).</summary>
public sealed record OrderResponse(
    int Id,
    string CustomerName,
    decimal TotalAmount,
    OrderStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<OrderItem> Items);

/// <summary>Paginated envelope returned by GET /api/orders (spec 04).</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);
