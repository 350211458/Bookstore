using Catalog;
using Grpc.Core;

namespace Catalog.Api.Services;

/// <summary>
/// gRPC surface of the catalog service (spec 03). Exposes <c>DeductStock</c> so the
/// Order service can atomically deduct stock during checkout.
/// </summary>
public sealed class CatalogGrpcService(StockService stockService) : CatalogService.CatalogServiceBase
{
    public override async Task<DeductStockResponse> DeductStock(
        DeductStockRequest request, ServerCallContext context)
    {
        // A non-positive quantity cannot be deducted; reject it rather than allow it to
        // inflate stock. The spec only defines deduction semantics, so any invalid input
        // reports failure.
        if (request.Quantity <= 0)
        {
            var current = await stockService.GetStockAsync(request.BookId);
            return new DeductStockResponse { Success = false, RemainingStock = current };
        }

        var result = await stockService.AdjustAsync(request.BookId, -request.Quantity);
        return new DeductStockResponse { Success = result.Success, RemainingStock = result.RemainingStock };
    }
}
