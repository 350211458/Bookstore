using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Catalog.Api.Tests;

/// <summary>
/// Integration tests for the /api/books REST endpoints (spec 03).
/// </summary>
public sealed class BooksEndpointsTests(CatalogApiFactory factory) : IClassFixture<CatalogApiFactory>
{
    private static async Task<JsonDocument> PostBookAsync(HttpClient client, object body)
    {
        var response = await client.PostAsJsonAsync("/api/books", body);
        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Created, $"Status {response.StatusCode}: {responseBody}");
        return JsonDocument.Parse(responseBody);
    }

    private static async Task<int> CreateBookAsync(
        HttpClient client, string title, string isbn, decimal price, int stock, string? category = null)
    {
        using var json = await PostBookAsync(client, new
        {
            title,
            author = "Test Author",
            isbn,
            price,
            stockQuantity = stock,
            category,
        });
        return json.RootElement.GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task Create_ValidBook_ReturnsCreatedWithSpecFields()
    {
        var client = factory.CreateClient();
        var isbn = $"978-{Guid.NewGuid():N}"[..16];

        using var json = await PostBookAsync(client, new
        {
            title = "Spec Test Book",
            author = "Alice Author",
            isbn,
            price = 29.99m,
            stockQuantity = 12,
            category = "Testing",
        });

        var root = json.RootElement;
        Assert.True(root.GetProperty("id").GetInt32() > 0);
        Assert.Equal("Spec Test Book", root.GetProperty("title").GetString());
        Assert.Equal("Alice Author", root.GetProperty("author").GetString());
        Assert.Equal(isbn, root.GetProperty("isbn").GetString());
        Assert.Equal(29.99m, root.GetProperty("price").GetDecimal());
        Assert.Equal(12, root.GetProperty("stockQuantity").GetInt32());
        Assert.Equal("Testing", root.GetProperty("category").GetString());
    }

    [Theory]
    [InlineData("", "978-1111111111", 10, 5)]     // empty title
    [InlineData("Book", "", 10, 5)]               // empty isbn
    [InlineData("Book", "978-1111111111", -1, 5)] // negative price
    [InlineData("Book", "978-1111111111", 10, -1)] // negative stock
    public async Task Create_InvalidBook_ReturnsBadRequest(
        string title, string isbn, decimal price, int stock)
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/books", new
        {
            title, author = "Author", isbn, price, stockQuantity = stock, category = "Testing",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_List_AppliesKeywordCategoryAndPriceFilters()
    {
        var client = factory.CreateClient();
        var prefix = Guid.NewGuid().ToString("N")[..8];

        await CreateBookAsync(client, $"{prefix} Alpha", $"978-{prefix}01", 10m, 5, "Fiction");
        await CreateBookAsync(client, $"{prefix} Beta", $"978-{prefix}02", 25m, 5, "Fiction");
        await CreateBookAsync(client, $"{prefix} Gamma", $"978-{prefix}03", 60m, 5, "History");

        // keyword filter matches the unique prefix -> exactly 3 books
        var keywordResponse = await client.GetAsync($"/api/books?keyword={prefix}");
        var keywordJson = JsonDocument.Parse(await keywordResponse.Content.ReadAsStringAsync());
        Assert.Equal(3, keywordJson.RootElement.GetProperty("totalCount").GetInt32());

        // category filter -> only Fiction books
        var categoryResponse = await client.GetAsync($"/api/books?keyword={prefix}&category=Fiction");
        var categoryJson = JsonDocument.Parse(await categoryResponse.Content.ReadAsStringAsync());
        Assert.Equal(2, categoryJson.RootElement.GetProperty("totalCount").GetInt32());

        // price range -> only Gamma (60)
        var priceResponse = await client.GetAsync($"/api/books?keyword={prefix}&minPrice=30&maxPrice=70");
        var priceJson = JsonDocument.Parse(await priceResponse.Content.ReadAsStringAsync());
        Assert.Equal(1, priceJson.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal($"{prefix} Gamma",
            priceJson.RootElement.GetProperty("items")[0].GetProperty("title").GetString());
    }

    [Fact]
    public async Task Get_ById_ReturnsBook_Or404()
    {
        var client = factory.CreateClient();
        var id = await CreateBookAsync(client, "ById Book", "978-2222222222", 15m, 3);

        var ok = await client.GetAsync($"/api/books/{id}");
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        using var json = JsonDocument.Parse(await ok.Content.ReadAsStringAsync());
        Assert.Equal("ById Book", json.RootElement.GetProperty("title").GetString());

        var missing = await client.GetAsync("/api/books/999999");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task Update_ValidBook_ReturnsUpdated()
    {
        var client = factory.CreateClient();
        var id = await CreateBookAsync(client, "Before Update", "978-3333333333", 10m, 3);

        var response = await client.PutAsJsonAsync($"/api/books/{id}", new
        {
            title = "After Update",
            author = "New Author",
            isbn = "978-3333333333",
            price = 42.5m,
            stockQuantity = 7,
            category = "Updated",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("After Update", json.RootElement.GetProperty("title").GetString());
        Assert.Equal(42.5m, json.RootElement.GetProperty("price").GetDecimal());
        Assert.Equal(7, json.RootElement.GetProperty("stockQuantity").GetInt32());
        Assert.Equal("Updated", json.RootElement.GetProperty("category").GetString());
    }

    [Fact]
    public async Task Update_MissingBook_Returns404()
    {
        var client = factory.CreateClient();
        var response = await client.PutAsJsonAsync("/api/books/999999", new
        {
            title = "Nope", author = "A", isbn = "978-4444444444", price = 1m, stockQuantity = 1, category = "X",
        });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_SoftDeletes_AndHidesFromListAndGet()
    {
        var client = factory.CreateClient();
        var id = await CreateBookAsync(client, "To Delete", "978-5555555555", 20m, 5);

        var deleteResponse = await client.DeleteAsync($"/api/books/{id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // hidden from get
        var getResponse = await client.GetAsync($"/api/books/{id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);

        // hidden from list
        var listResponse = await client.GetAsync($"/api/books?keyword=To Delete");
        var listJson = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
        Assert.Equal(0, listJson.RootElement.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task AdjustStock_PositiveDelta_IncreasesStock()
    {
        var client = factory.CreateClient();
        var id = await CreateBookAsync(client, "Stock Up", "978-6666666666", 10m, 4);

        var response = await client.PatchAsJsonAsync($"/api/books/{id}/stock", new { delta = 6 });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(10, json.RootElement.GetProperty("stockQuantity").GetInt32());
    }

    [Fact]
    public async Task AdjustStock_NegativeDelta_DecreasesStock()
    {
        var client = factory.CreateClient();
        var id = await CreateBookAsync(client, "Stock Down", "978-7777777777", 10m, 4);

        var response = await client.PatchAsJsonAsync($"/api/books/{id}/stock", new { delta = -3 });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(1, json.RootElement.GetProperty("stockQuantity").GetInt32());
    }

    [Fact]
    public async Task AdjustStock_NegativeBeyondStock_ReturnsBadRequest()
    {
        var client = factory.CreateClient();
        var id = await CreateBookAsync(client, "Stock Floor", "978-8888888888", 10m, 2);

        var response = await client.PatchAsJsonAsync($"/api/books/{id}/stock", new { delta = -5 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        // stock unchanged
        var get = await client.GetAsync($"/api/books/{id}");
        using var json = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        Assert.Equal(2, json.RootElement.GetProperty("stockQuantity").GetInt32());
    }

    [Fact]
    public async Task AdjustStock_MissingBook_Returns404()
    {
        var client = factory.CreateClient();
        var response = await client.PatchAsJsonAsync("/api/books/999999/stock", new { delta = 1 });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
