using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Order.Api.Tests;

/// <summary>
/// Integration tests for /api/cart (spec 04). Each test uses a unique session id so
/// the shared per-class SQLite database stays isolated.
/// </summary>
public sealed class CartEndpointsTests(OrderApiFactory factory) : IClassFixture<OrderApiFactory>
{
    private static Task<HttpResponseMessage> AddItemAsync(
        HttpClient client, string sessionId, int bookId, string title, decimal unitPrice, int quantity) =>
        client.PostAsJsonAsync("/api/cart/items", new { sessionId, bookId, title, unitPrice, quantity });

    private static async Task<JsonDocument> GetCartAsync(HttpClient client, string sessionId)
    {
        var response = await client.GetAsync($"/api/cart?sessionId={sessionId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Add_NewItem_AddsLine()
    {
        var client = factory.CreateClient();
        var sessionId = Guid.NewGuid().ToString("N");

        var response = await AddItemAsync(client, sessionId, bookId: 1, title: "Clean Code", unitPrice: 39.99m, quantity: 2);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.Equal(1, root.GetProperty("bookId").GetInt32());
        Assert.Equal("Clean Code", root.GetProperty("title").GetString());
        Assert.Equal(39.99m, root.GetProperty("unitPrice").GetDecimal());
        Assert.Equal(2, root.GetProperty("quantity").GetInt32());
    }

    [Fact]
    public async Task Add_ExistingItem_IncrementsQuantity()
    {
        var client = factory.CreateClient();
        var sessionId = Guid.NewGuid().ToString("N");

        await AddItemAsync(client, sessionId, bookId: 1, title: "Clean Code", unitPrice: 39.99m, quantity: 2);
        var response = await AddItemAsync(client, sessionId, bookId: 1, title: "Clean Code", unitPrice: 39.99m, quantity: 3);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(5, json.RootElement.GetProperty("quantity").GetInt32());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public async Task Add_InvalidQuantity_Returns400(int quantity)
    {
        var client = factory.CreateClient();
        var sessionId = Guid.NewGuid().ToString("N");

        var response = await AddItemAsync(client, sessionId, bookId: 1, title: "Clean Code", unitPrice: 39.99m, quantity);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateQuantity_SetsQuantity()
    {
        var client = factory.CreateClient();
        var sessionId = Guid.NewGuid().ToString("N");
        await AddItemAsync(client, sessionId, bookId: 1, title: "Clean Code", unitPrice: 39.99m, quantity: 2);

        var response = await client.PatchAsJsonAsync($"/api/cart/items/1?sessionId={sessionId}", new { quantity = 7 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(7, json.RootElement.GetProperty("quantity").GetInt32());
    }

    [Fact]
    public async Task UpdateQuantity_InvalidQuantity_Returns400()
    {
        var client = factory.CreateClient();
        var sessionId = Guid.NewGuid().ToString("N");
        await AddItemAsync(client, sessionId, bookId: 1, title: "Clean Code", unitPrice: 39.99m, quantity: 2);

        var response = await client.PatchAsJsonAsync($"/api/cart/items/1?sessionId={sessionId}", new { quantity = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateQuantity_MissingLine_Returns404()
    {
        var client = factory.CreateClient();
        var sessionId = Guid.NewGuid().ToString("N");

        var response = await client.PatchAsJsonAsync($"/api/cart/items/999999?sessionId={sessionId}", new { quantity = 3 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RemoveItem_RemovesLine()
    {
        var client = factory.CreateClient();
        var sessionId = Guid.NewGuid().ToString("N");
        await AddItemAsync(client, sessionId, bookId: 1, title: "Clean Code", unitPrice: 39.99m, quantity: 2);

        var response = await client.DeleteAsync($"/api/cart/items/1?sessionId={sessionId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var json = await GetCartAsync(client, sessionId);
        Assert.Equal(0, json.RootElement.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task RemoveItem_MissingLine_Returns404()
    {
        var client = factory.CreateClient();
        var sessionId = Guid.NewGuid().ToString("N");

        var response = await client.DeleteAsync($"/api/cart/items/999999?sessionId={sessionId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Clear_EmptiesCart()
    {
        var client = factory.CreateClient();
        var sessionId = Guid.NewGuid().ToString("N");
        await AddItemAsync(client, sessionId, bookId: 1, title: "Clean Code", unitPrice: 39.99m, quantity: 2);
        await AddItemAsync(client, sessionId, bookId: 2, title: "DDD", unitPrice: 59.50m, quantity: 1);

        var response = await client.DeleteAsync($"/api/cart?sessionId={sessionId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var json = await GetCartAsync(client, sessionId);
        Assert.Equal(0, json.RootElement.GetProperty("items").GetArrayLength());
        Assert.Equal(0m, json.RootElement.GetProperty("totalAmount").GetDecimal());
    }

    [Fact]
    public async Task GetCart_ComputesTotal()
    {
        var client = factory.CreateClient();
        var sessionId = Guid.NewGuid().ToString("N");
        await AddItemAsync(client, sessionId, bookId: 1, title: "Clean Code", unitPrice: 39.99m, quantity: 2);
        await AddItemAsync(client, sessionId, bookId: 2, title: "DDD", unitPrice: 59.50m, quantity: 1);

        using var json = await GetCartAsync(client, sessionId);
        var root = json.RootElement;
        Assert.Equal(2, root.GetProperty("items").GetArrayLength());
        Assert.Equal(139.48m, root.GetProperty("totalAmount").GetDecimal());
    }
}
