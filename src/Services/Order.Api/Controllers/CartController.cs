using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Order.Api.Data;
using Order.Api.Models;

namespace Order.Api.Controllers;

/// <summary>
/// Shopping-cart endpoints (spec 04): <c>/api/cart</c>, keyed by SessionId.
/// </summary>
[ApiController]
[Route("api/cart")]
public sealed class CartController(OrderDbContext db) : ControllerBase
{
    /// <summary>Cart lines plus the computed total for a session.</summary>
    [HttpGet]
    public async Task<IActionResult> GetCart([FromQuery] string? sessionId)
    {
        var items = sessionId is null
            ? new List<CartItem>()
            : await db.CartItems.Where(c => c.SessionId == sessionId).OrderBy(c => c.BookId).ToListAsync();

        return Ok(new CartResponse(items, items.Sum(i => i.UnitPrice * i.Quantity)));
    }

    /// <summary>Add a line, or increment the quantity when the book is already in the cart.</summary>
    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] AddCartItemRequest request)
    {
        var existing = await db.CartItems.FirstOrDefaultAsync(
            c => c.SessionId == request.SessionId && c.BookId == request.BookId);

        if (existing is not null)
        {
            existing.Quantity += request.Quantity;
            await db.SaveChangesAsync();
            return Ok(existing);
        }

        var item = new CartItem
        {
            SessionId = request.SessionId,
            BookId = request.BookId,
            Title = request.Title,
            UnitPrice = request.UnitPrice,
            Quantity = request.Quantity,
        };

        db.CartItems.Add(item);
        await db.SaveChangesAsync();
        return Ok(item);
    }

    /// <summary>Set a line's quantity; 404 when the line is missing.</summary>
    [HttpPatch("items/{bookId:int}")]
    public async Task<IActionResult> UpdateQuantity(
        int bookId, [FromQuery] string? sessionId, [FromBody] UpdateCartItemQuantityRequest request)
    {
        var item = await db.CartItems.FirstOrDefaultAsync(
            c => c.SessionId == sessionId && c.BookId == bookId);
        if (item is null)
        {
            return NotFound();
        }

        item.Quantity = request.Quantity;
        await db.SaveChangesAsync();
        return Ok(item);
    }

    /// <summary>Remove a line; 404 when the line is missing.</summary>
    [HttpDelete("items/{bookId:int}")]
    public async Task<IActionResult> RemoveItem(int bookId, [FromQuery] string? sessionId)
    {
        var item = await db.CartItems.FirstOrDefaultAsync(
            c => c.SessionId == sessionId && c.BookId == bookId);
        if (item is null)
        {
            return NotFound();
        }

        db.CartItems.Remove(item);
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Clear the whole cart for a session.</summary>
    [HttpDelete]
    public async Task<IActionResult> Clear([FromQuery] string? sessionId)
    {
        db.CartItems.RemoveRange(db.CartItems.Where(c => c.SessionId == sessionId));
        await db.SaveChangesAsync();
        return NoContent();
    }
}
