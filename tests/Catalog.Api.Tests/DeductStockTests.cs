using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Catalog;
using Grpc.Net.Client;

namespace Catalog.Api.Tests;

/// <summary>
/// Integration tests for the gRPC DeductStock endpoint (spec 03).
/// </summary>
public sealed class DeductStockTests(CatalogApiFactory factory) : IClassFixture<CatalogApiFactory>
{
    private static async Task<int> CreateBookAsync(
        HttpClient client, string title, string isbn, decimal price, int stock, string? category = null)
    {
        var response = await client.PostAsJsonAsync("/api/books", new
        {
            title,
            author = "Test Author",
            isbn,
            price,
            stockQuantity = stock,
            category,
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("id").GetInt32();
    }

    private static async Task<int> GetStockAsync(HttpClient client, int bookId)
    {
        var get = await client.GetAsync($"/api/books/{bookId}");
        using var json = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("stockQuantity").GetInt32();
    }

    /// <summary>
    /// Creates a gRPC channel that talks to the in-memory TestServer over HTTP/2.
    /// </summary>
    private GrpcChannel CreateChannel()
    {
        var httpClient = factory.CreateClient();
        httpClient.DefaultRequestVersion = HttpVersion.Version20;
        httpClient.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
        return GrpcChannel.ForAddress(httpClient.BaseAddress!, new GrpcChannelOptions { HttpClient = httpClient });
    }

    [Fact]
    public async Task DeductStock_SufficientStock_ReturnsSuccessAndRemainingStock()
    {
        var client = factory.CreateClient();
        var bookId = await CreateBookAsync(client, "Deduct Success", "978-1111111110", 10m, 7);

        using var channel = CreateChannel();
        var grpc = new CatalogService.CatalogServiceClient(channel);

        var response = await grpc.DeductStockAsync(new DeductStockRequest { BookId = bookId, Quantity = 3 });

        Assert.True(response.Success);
        Assert.Equal(4, response.RemainingStock);

        // the REST view reflects the deducted stock
        Assert.Equal(4, await GetStockAsync(client, bookId));
    }

    [Fact]
    public async Task DeductStock_InsufficientStock_ReturnsFailure()
    {
        var client = factory.CreateClient();
        var bookId = await CreateBookAsync(client, "Deduct Fail", "978-1111111111", 10m, 2);

        using var channel = CreateChannel();
        var grpc = new CatalogService.CatalogServiceClient(channel);

        var response = await grpc.DeductStockAsync(new DeductStockRequest { BookId = bookId, Quantity = 5 });

        Assert.False(response.Success);
        Assert.Equal(2, response.RemainingStock);

        // stock is unchanged
        Assert.Equal(2, await GetStockAsync(client, bookId));
    }

    [Fact]
    public async Task DeductStock_MissingBook_ReturnsFailure()
    {
        using var channel = CreateChannel();
        var grpc = new CatalogService.CatalogServiceClient(channel);

        var response = await grpc.DeductStockAsync(new DeductStockRequest { BookId = 999999, Quantity = 1 });

        Assert.False(response.Success);
        Assert.Equal(0, response.RemainingStock);
    }

    [Fact]
    public async Task DeductStock_SoftDeletedBook_ReturnsFailure()
    {
        var client = factory.CreateClient();
        var bookId = await CreateBookAsync(client, "Deduct Deleted", "978-1111111112", 10m, 5);

        var deleteResponse = await client.DeleteAsync($"/api/books/{bookId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        using var channel = CreateChannel();
        var grpc = new CatalogService.CatalogServiceClient(channel);

        var response = await grpc.DeductStockAsync(new DeductStockRequest { BookId = bookId, Quantity = 1 });

        Assert.False(response.Success);
        Assert.Equal(0, response.RemainingStock);
    }

    [Fact]
    public async Task DeductStock_Concurrent_DoesNotOversell()
    {
        var client = factory.CreateClient();
        var bookId = await CreateBookAsync(client, "Deduct Concurrent", "978-1111111113", 10m, 5);

        using var channel = CreateChannel();
        var grpc = new CatalogService.CatalogServiceClient(channel);

        // 10 concurrent deductions of 1 against a stock of 5: exactly 5 must succeed,
        // 5 must fail, and the final stock must be 0 (never negative).
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => grpc.DeductStockAsync(new DeductStockRequest { BookId = bookId, Quantity = 1 }).ResponseAsync)
            .ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.Equal(5, results.Count(r => r.Success));
        Assert.Equal(5, results.Count(r => !r.Success));
        Assert.All(results.Where(r => r.Success), r => Assert.True(r.RemainingStock >= 0));

        Assert.Equal(0, await GetStockAsync(client, bookId));
    }
}
