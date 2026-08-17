using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Order.Api.Data;
using Order.Api.Models;
using Order.Api.Services;
// The entity type Order collides with the Order namespace segment of the project's root
// namespace (Order.Api.*), so reference it through an explicit alias.
using OrderEntity = Order.Api.Models.Order;

namespace Order.Api.Controllers;

/// <summary>
/// Order endpoints (spec 04): list, detail, checkout and the order state machine.
/// </summary>
[ApiController]
[Route("api/orders")]
public sealed class OrdersController(OrderDbContext db, IStockDeductionService stockDeduction) : ControllerBase
{
    /// <summary>Paginated list of orders.</summary>
    [HttpGet]
    public async Task<IActionResult> GetOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Orders;
        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(o => o.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new PagedResult<OrderEntity>(items, totalCount, page, pageSize));
    }

    /// <summary>Order with its items; 404 when missing.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == id);
        if (order is null)
        {
            return NotFound();
        }

        var items = await db.OrderItems.Where(oi => oi.OrderId == id).OrderBy(oi => oi.BookId).ToListAsync();
        return Ok(ToResponse(order, items));
    }

    /// <summary>
    /// Checkout: deducts stock for every cart line via the catalog gRPC DeductStock;
    /// on any failure no order is created and 409 is returned.
    /// </summary>
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
    {
        var cartItems = await db.CartItems
            .Where(c => c.SessionId == request.SessionId)
            .OrderBy(c => c.BookId)
            .ToListAsync();

        if (cartItems.Count == 0)
        {
            return BadRequest("Cart is empty.");
        }

        foreach (var item in cartItems)
        {
            var result = await stockDeduction.DeductAsync(item.BookId, item.Quantity);
            if (!result.Success)
            {
                // Abort; compensating restock of already-deducted lines is out of scope
                // (spec 04) — Catalog has no restock RPC yet.
                return Conflict("Stock deduction failed for a cart item.");
            }
        }

        var orderItems = cartItems
            .Select(i => new OrderItem
            {
                BookId = i.BookId,
                Title = i.Title,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity,
                LineTotal = i.UnitPrice * i.Quantity,
            })
            .ToList();

        var now = DateTime.UtcNow;
        var order = new OrderEntity
        {
            CustomerName = request.CustomerName,
            TotalAmount = orderItems.Sum(oi => oi.LineTotal),
            Status = OrderStatus.Placed,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        foreach (var oi in orderItems)
        {
            oi.OrderId = order.Id;
        }

        db.OrderItems.AddRange(orderItems);
        await db.SaveChangesAsync();

        db.CartItems.RemoveRange(db.CartItems.Where(c => c.SessionId == request.SessionId));
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, ToResponse(order, orderItems));
    }

    /// <summary>Transition status; only allowed transitions are accepted, otherwise 400.</summary>
    [HttpPost("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusRequest request)
    {
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == id);
        if (order is null)
        {
            return NotFound();
        }

        if (!OrderStateMachine.CanTransition(order.Status, request.Status))
        {
            return BadRequest($"Cannot transition from {order.Status} to {request.Status}.");
        }

        order.Status = request.Status;
        order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(order);
    }

    /// <summary>Shorthand for → Cancelled from Placed/Paid; otherwise 400.</summary>
    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == id);
        if (order is null)
        {
            return NotFound();
        }

        if (!OrderStateMachine.CanTransition(order.Status, OrderStatus.Cancelled))
        {
            return BadRequest($"Order cannot be cancelled from status {order.Status}.");
        }

        order.Status = OrderStatus.Cancelled;
        order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(order);
    }

    private static OrderResponse ToResponse(OrderEntity order, IReadOnlyList<OrderItem> items) =>
        new(order.Id, order.CustomerName, order.TotalAmount, order.Status,
            order.CreatedAt, order.UpdatedAt, items);
}
