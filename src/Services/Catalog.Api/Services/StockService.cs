using Catalog.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Services;

/// <summary>Outcome of a stock adjustment (spec 03).</summary>
public readonly record struct StockAdjustResult(int RemainingStock, bool Success);

/// <summary>
/// Atomic stock adjustments (spec 03). Every write is a single conditional UPDATE
/// statement whose WHERE clause guards the stock floor, so concurrent deductions cannot
/// oversell (the database serializes writers on both PostgreSQL and SQLite).
/// </summary>
public sealed class StockService(CatalogDbContext db)
{
    /// <summary>
    /// Adjusts a book's stock by <paramref name="delta"/> (negative deducts).
    /// Returns <c>Success=false</c> when the book is missing/soft-deleted or the
    /// adjustment would drive stock below zero.
    /// </summary>
    public async Task<StockAdjustResult> AdjustAsync(int bookId, int delta)
    {
        if (delta == 0)
        {
            var current = await db.Books
                .Where(b => b.Id == bookId && !b.IsDeleted)
                .Select(b => (int?)b.StockQuantity)
                .FirstOrDefaultAsync();

            return current is null
                ? new StockAdjustResult(0, false)
                : new StockAdjustResult(current.Value, true);
        }

        // Single conditional UPDATE statement: atomic, race-free stock floor.
        var updated = delta > 0
            ? await db.Books
                .Where(b => b.Id == bookId && !b.IsDeleted)
                .ExecuteUpdateAsync(s => s.SetProperty(b => b.StockQuantity, b => b.StockQuantity + delta))
            : await db.Books
                .Where(b => b.Id == bookId && !b.IsDeleted && b.StockQuantity + delta >= 0)
                .ExecuteUpdateAsync(s => s.SetProperty(b => b.StockQuantity, b => b.StockQuantity + delta));

        if (updated == 0)
        {
            var remaining = await db.Books
                .Where(b => b.Id == bookId && !b.IsDeleted)
                .Select(b => (int?)b.StockQuantity)
                .FirstOrDefaultAsync();

            return new StockAdjustResult(remaining ?? 0, false);
        }

        var newStock = await db.Books
            .Where(b => b.Id == bookId)
            .Select(b => (int)b.StockQuantity)
            .FirstAsync();

        return new StockAdjustResult(newStock, true);
    }

    /// <summary>Current stock of a book, or 0 when missing/soft-deleted.</summary>
    public async Task<int> GetStockAsync(int bookId)
    {
        return await db.Books
            .Where(b => b.Id == bookId && !b.IsDeleted)
            .Select(b => b.StockQuantity)
            .FirstOrDefaultAsync();
    }
}
