namespace Order.Api.Services;

/// <summary>Outcome of a stock-deduction attempt against the catalog (spec 04).</summary>
public readonly record struct StockDeductionResult(bool Success, int RemainingStock);

/// <summary>
/// Seam over the catalog gRPC DeductStock call so checkout logic is testable with a stub
/// (spec 04: "Wrap the gRPC client behind IStockDeductionService").
/// </summary>
public interface IStockDeductionService
{
    Task<StockDeductionResult> DeductAsync(int bookId, int quantity);
}
