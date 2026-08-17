using Catalog;
using Grpc.Net.Client;

namespace Order.Api.Services;

/// <summary>
/// Real implementation of <see cref="IStockDeductionService"/>: calls Catalog.Api's gRPC
/// DeductStock endpoint (spec 04). The channel address is supplied from configuration.
/// </summary>
public sealed class CatalogStockDeductionService(GrpcChannel channel) : IStockDeductionService
{
    private readonly CatalogService.CatalogServiceClient _client = new(channel);

    public async Task<StockDeductionResult> DeductAsync(int bookId, int quantity)
    {
        var response = await _client.DeductStockAsync(new DeductStockRequest
        {
            BookId = bookId,
            Quantity = quantity,
        });

        return new StockDeductionResult(response.Success, response.RemainingStock);
    }
}
