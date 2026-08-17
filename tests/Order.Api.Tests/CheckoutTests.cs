using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Order.Api.Services;

namespace Order.Api.Tests;

/// <summary>
/// Integration tests for POST /api/orders/checkout (spec 04). The stock deduction is a
/// stubbed seam; a failure mid-checkout must produce no order and keep the cart intact.
/// </summary>
public sealed class CheckoutTests(OrderApiFactory factory) : IClassFixture<OrderApiFactory>
{
    private static Task<HttpResponseMessage> AddItemAsync(
        HttpClient client, string sessionId, int bookId, string title, decimal unitPrice, int quantity) =>
        client.PostAsJsonAsync("/api/cart/items", new { sessionId, bookId, title, unitPrice, quantity });

    private static Task<HttpResponseMessage> CheckoutAsync(
        HttpClient client, string sessionId, string customerName) =>
        client.PostAsJsonAsync("/api/orders/checkout", new { sessionId, customerName });

    private static async Task<JsonDocument> GetCartAsync(HttpClient client, string sessionId)
    {
        var response = await client.GetAsync($"/api/cart?sessionId={sessionId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Checkout_Success_CreatesOrderClearsCartAndDeductsEachLine()
    {
        factory.StockDeduction.Reset();
        var client = factory.CreateClient();
        var sessionId = Guid.NewGuid().ToString("N");
        var customerName = $"Cust-{Guid.NewGuid():N}";

        await AddItemAsync(client, sessionId, bookId: 1, title: "Clean Code", unitPrice: 39.99m, quantity: 2);
        await AddItemAsync(client, sessionId, bookId: 2, title: "DDD", unitPrice: 59.50m, quantity: 1);

        var response = await CheckoutAsync(client, sessionId, customerName);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.Equal(customerName, root.GetProperty("customerName").GetString());
        Assert.Equal(139.48m, root.GetProperty("totalAmount").GetDecimal()); // 39.99*2 + 59.50
        Assert.Equal(0, root.GetProperty("status").GetInt32()); // OrderStatus.Placed
        Assert.Equal(2, root.GetProperty("items").GetArrayLength());

        var first = root.GetProperty("items")[0];
        Assert.Equal(1, first.GetProperty("bookId").GetInt32());
        Assert.Equal(79.98m, first.GetProperty("lineTotal").GetDecimal());

        // the cart is cleared after a successful checkout
        using var cart = await GetCartAsync(client, sessionId);
        Assert.Equal(0, cart.RootElement.GetProperty("items").GetArrayLength());

        // one deduction per cart line, in book order
        Assert.Equal(2, factory.StockDeduction.Calls.Count);
        Assert.Contains(factory.StockDeduction.Calls, c => c.BookId == 1 && c.Quantity == 2);
        Assert.Contains(factory.StockDeduction.Calls, c => c.BookId == 2 && c.Quantity == 1);
    }

    [Fact]
    public async Task Checkout_EmptyCart_Returns400()
    {
        factory.StockDeduction.Reset();
        var client = factory.CreateClient();
        var sessionId = Guid.NewGuid().ToString("N");

        var response = await CheckoutAsync(client, sessionId, $"Cust-{Guid.NewGuid():N}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(factory.StockDeduction.Calls);
    }

    [Fact]
    public async Task Checkout_DeductionFailure_Returns409CreatesNoOrderAndKeepsCart()
    {
        factory.StockDeduction.Reset();
        var client = factory.CreateClient();
        var sessionId = Guid.NewGuid().ToString("N");
        var customerName = $"Cust-{Guid.NewGuid():N}";

        await AddItemAsync(client, sessionId, bookId: 1, title: "Clean Code", unitPrice: 39.99m, quantity: 2);
        await AddItemAsync(client, sessionId, bookId: 2, title: "DDD", unitPrice: 59.50m, quantity: 1);

        factory.StockDeduction.Handler = static (_, _) => new StockDeductionResult(false, 0);
        try
        {
            var response = await CheckoutAsync(client, sessionId, customerName);
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }
        finally
        {
            factory.StockDeduction.Reset();
        }

        // no order with this customer exists
        var orders = await client.GetAsync("/api/orders?page=1&pageSize=100");
        using var ordersJson = JsonDocument.Parse(await orders.Content.ReadAsStringAsync());
        Assert.DoesNotContain(
            ordersJson.RootElement.GetProperty("items").EnumerateArray(),
            item => item.GetProperty("customerName").GetString() == customerName);

        // the cart is untouched
        using var cart = await GetCartAsync(client, sessionId);
        Assert.Equal(2, cart.RootElement.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task Checkout_CreatedOrder_AppearsInPaginatedList()
    {
        factory.StockDeduction.Reset();
        var client = factory.CreateClient();
        var sessionId = Guid.NewGuid().ToString("N");
        var customerName = $"Cust-{Guid.NewGuid():N}";

        await AddItemAsync(client, sessionId, bookId: 1, title: "Clean Code", unitPrice: 39.99m, quantity: 1);
        await CheckoutAsync(client, sessionId, customerName);

        var response = await client.GetAsync("/api/orders?page=1&pageSize=20");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Contains(
            json.RootElement.GetProperty("items").EnumerateArray(),
            item => item.GetProperty("customerName").GetString() == customerName);
    }
}
