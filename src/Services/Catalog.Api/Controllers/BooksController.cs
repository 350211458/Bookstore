using Catalog.Api.Data;
using Catalog.Api.Models;
using Catalog.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Controllers;

/// <summary>
/// Catalog management REST endpoints (spec 03): <c>/api/books</c>.
/// </summary>
[ApiController]
[Route("api/books")]
public sealed class BooksController(CatalogDbContext db, StockService stockService) : ControllerBase
{
    /// <summary>
    /// Paginated list with optional keyword / category / minPrice / maxPrice filters.
    /// Soft-deleted books are hidden.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetBooks(
        [FromQuery] string? keyword,
        [FromQuery] string? category,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Books.Where(b => !b.IsDeleted);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(b =>
                b.Title.Contains(keyword) || b.Author.Contains(keyword) || b.ISBN.Contains(keyword));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(b => b.Category == category);
        }

        if (minPrice is not null)
        {
            query = query.Where(b => b.Price >= minPrice);
        }

        if (maxPrice is not null)
        {
            query = query.Where(b => b.Price <= maxPrice);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(b => b.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new PagedResult<Book>(items, totalCount, page, pageSize));
    }

    /// <summary>Single book; 404 when missing or soft-deleted.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var book = await db.Books.FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);
        return book is null ? NotFound() : Ok(book);
    }

    /// <summary>Create a book. Requires non-empty Title/ISBN, Price &gt;= 0, StockQuantity &gt;= 0.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBookRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.ISBN))
        {
            return BadRequest("Title and ISBN must not be empty.");
        }

        var book = new Book
        {
            Title = request.Title,
            Author = request.Author,
            ISBN = request.ISBN,
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            Category = request.Category,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        db.Books.Add(book);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = book.Id }, book);
    }

    /// <summary>Update a book; 404 when missing or soft-deleted.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateBookRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.ISBN))
        {
            return BadRequest("Title and ISBN must not be empty.");
        }

        var book = await db.Books.FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);
        if (book is null)
        {
            return NotFound();
        }

        book.Title = request.Title;
        book.Author = request.Author;
        book.ISBN = request.ISBN;
        book.Price = request.Price;
        book.StockQuantity = request.StockQuantity;
        book.Category = request.Category;
        book.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return Ok(book);
    }

    /// <summary>Soft delete a book (sets IsDeleted); hidden from list/get afterwards.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var book = await db.Books.FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);
        if (book is null)
        {
            return NotFound();
        }

        book.IsDeleted = true;
        book.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>Adjust stock by a delta; the resulting quantity may not go below zero.</summary>
    [HttpPatch("{id:int}/stock")]
    public async Task<IActionResult> AdjustStock(int id, [FromBody] StockAdjustRequest request)
    {
        var result = await stockService.AdjustAsync(id, request.Delta);

        if (!result.Success)
        {
            var exists = await db.Books.AnyAsync(b => b.Id == id && !b.IsDeleted);
            return exists
                ? BadRequest("Adjustment would take stock below zero.")
                : NotFound();
        }

        var book = await db.Books.FirstAsync(b => b.Id == id && !b.IsDeleted);
        return Ok(book);
    }
}
