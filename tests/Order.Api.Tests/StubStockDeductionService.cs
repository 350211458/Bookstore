using Order.Api.Services;

namespace Order.Api.Tests;

/// <summary>
/// Configurable stand-in for <see cref="IStockDeductionService"/> (spec 04): tests set
/// <see cref="Handler"/> to control success/failure and inspect <see cref="Calls"/> to
/// assert which lines were deducted.
/// </summary>
public sealed class StubStockDeductionService : IStockDeductionService
{
    /// <summary>Current behavior; default is a successful deduction with an arbitrary remaining stock.</summary>
    public Func<int, int, StockDeductionResult> Handler { get; set; }
        = static (_, _) => new StockDeductionResult(true, 1);

    /// <summary>Every (BookId, Quantity) handed to DeductAsync, in call order.</summary>
    public List<(int BookId, int Quantity)> Calls { get; } = new();

    public Task<StockDeductionResult> DeductAsync(int bookId, int quantity)
    {
        Calls.Add((bookId, quantity));
        return Task.FromResult(Handler(bookId, quantity));
    }

    /// <summary>Restores the default success behavior and clears the call log.</summary>
    public void Reset()
    {
        Calls.Clear();
        Handler = static (_, _) => new StockDeductionResult(true, 1);
    }
}
