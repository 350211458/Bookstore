using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Order.Api.Tests;

/// <summary>
/// Integration tests for the order state machine (spec 04):
/// status transitions and the /cancel shorthand, backed by a real checkout.
/// </summary>
public sealed class OrderStateMachineTests(OrderApiFactory factory) : IClassFixture<OrderApiFactory>
{
    /// <summary>Adds one cart line and checks out, returning the created order id.</summary>
    private static async Task<int> CreateOrderAsync(HttpClient client, string sessionId, string customerName)
    {
        var add = await client.PostAsJsonAsync("/api/cart/items",
            new { sessionId, bookId = 1, title = "Clean Code", unitPrice = 39.99m, quantity = 1 });
        Assert.Equal(HttpStatusCode.OK, add.StatusCode);

        var checkout = await client.PostAsJsonAsync("/api/orders/checkout", new { sessionId, customerName });
        Assert.Equal(HttpStatusCode.Created, checkout.StatusCode);
        using var json = JsonDocument.Parse(await checkout.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("id").GetInt32();
    }

    private static async Task<int> GetStatusAsync(HttpClient client, int orderId)
    {
        var response = await client.GetAsync($"/api/orders/{orderId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("status").GetInt32();
    }

    /// <summary>Posts a status transition and asserts it is accepted with the expected result.</summary>
    private static async Task AssertTransitionAsync(HttpClient client, int orderId, int expectedStatus)
    {
        var response = await client.PostAsJsonAsync($"/api/orders/{orderId}/status", new { status = expectedStatus });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(expectedStatus, json.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task ValidChain_PlacedToCompleted()
    {
        var client = factory.CreateClient();
        var orderId = await CreateOrderAsync(client, Guid.NewGuid().ToString("N"), $"Cust-{Guid.NewGuid():N}");

        // Placed(0) -> Paid(1) -> Processing(2) -> Shipped(3) -> Completed(4)
        await AssertTransitionAsync(client, orderId, 1);
        await AssertTransitionAsync(client, orderId, 2);
        await AssertTransitionAsync(client, orderId, 3);
        await AssertTransitionAsync(client, orderId, 4);

        // Completed is a terminal state
        Assert.Equal(4, await GetStatusAsync(client, orderId));
    }

    [Fact]
    public async Task InvalidTransition_Returns400AndStateUnchanged()
    {
        var client = factory.CreateClient();
        var orderId = await CreateOrderAsync(client, Guid.NewGuid().ToString("N"), $"Cust-{Guid.NewGuid():N}");

        // Placed -> Shipped is not allowed
        var response = await client.PostAsJsonAsync($"/api/orders/{orderId}/status", new { status = 3 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await GetStatusAsync(client, orderId)); // still Placed
    }

    [Fact]
    public async Task Cancel_FromPlaced_ReturnsCancelled()
    {
        var client = factory.CreateClient();
        var orderId = await CreateOrderAsync(client, Guid.NewGuid().ToString("N"), $"Cust-{Guid.NewGuid():N}");

        var response = await client.PostAsync($"/api/orders/{orderId}/cancel", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(5, await GetStatusAsync(client, orderId)); // Cancelled
    }

    [Fact]
    public async Task Cancel_FromPaid_ReturnsCancelled()
    {
        var client = factory.CreateClient();
        var orderId = await CreateOrderAsync(client, Guid.NewGuid().ToString("N"), $"Cust-{Guid.NewGuid():N}");
        await AssertTransitionAsync(client, orderId, 1); // Paid

        var response = await client.PostAsync($"/api/orders/{orderId}/cancel", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(5, await GetStatusAsync(client, orderId)); // Cancelled
    }

    [Fact]
    public async Task Cancel_FromCompleted_Returns400()
    {
        var client = factory.CreateClient();
        var orderId = await CreateOrderAsync(client, Guid.NewGuid().ToString("N"), $"Cust-{Guid.NewGuid():N}");
        await AssertTransitionAsync(client, orderId, 1); // Paid
        await AssertTransitionAsync(client, orderId, 2); // Processing
        await AssertTransitionAsync(client, orderId, 3); // Shipped
        await AssertTransitionAsync(client, orderId, 4); // Completed

        var response = await client.PostAsync($"/api/orders/{orderId}/cancel", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(4, await GetStatusAsync(client, orderId)); // still Completed
    }
}
