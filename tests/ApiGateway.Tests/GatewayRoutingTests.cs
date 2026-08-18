using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ApiGateway.Tests;

/// <summary>
/// Integration tests for the YARP gateway (spec 05): each service prefix routes to its own
/// cluster, the prefix is stripped so the upstream receives its native path, HTTP method and
/// query string pass through unchanged, and unmatched paths return 404.
/// </summary>
public sealed class GatewayRoutingTests(ApiGatewayFactory factory) : IClassFixture<ApiGatewayFactory>
{
    [Fact]
    public async Task Identity_Route_StripsPrefixAndForwardsToIdentityCluster()
    {
        factory.Identity.Clear();
        var client = factory.CreateClient();

        var response = await client.PostAsync("/identity/connect/token", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var recorded = Assert.Single(factory.Identity.Requests);
        Assert.Equal("POST", recorded.Method);
        Assert.Equal("/connect/token", recorded.Path);
    }

    [Fact]
    public async Task Catalog_Route_StripsPrefixAndForwardsToCatalogCluster()
    {
        factory.Catalog.Clear();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/catalog/api/books");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var recorded = Assert.Single(factory.Catalog.Requests);
        Assert.Equal("GET", recorded.Method);
        Assert.Equal("/api/books", recorded.Path);
    }

    [Fact]
    public async Task Catalog_StockPatch_ForwardsMethodAndNativePath()
    {
        factory.Catalog.Clear();
        var client = factory.CreateClient();

        var response = await client.PatchAsync("/catalog/api/books/1/stock", JsonContent.Create(new { delta = -2 }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var recorded = Assert.Single(factory.Catalog.Requests);
        Assert.Equal("PATCH", recorded.Method);
        Assert.Equal("/api/books/1/stock", recorded.Path);
    }

    [Fact]
    public async Task Order_Cart_ForwardsQueryString()
    {
        factory.Order.Clear();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/order/api/cart?sessionId=abc123");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var recorded = Assert.Single(factory.Order.Requests);
        Assert.Equal("GET", recorded.Method);
        Assert.Equal("/api/cart", recorded.Path);
        Assert.Equal("?sessionId=abc123", recorded.Query);
    }

    [Fact]
    public async Task Order_Checkout_ForwardsToNativePath()
    {
        factory.Order.Clear();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/order/api/orders/checkout", new { sessionId = "s1", customerName = "Cust" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var recorded = Assert.Single(factory.Order.Requests);
        Assert.Equal("POST", recorded.Method);
        Assert.Equal("/api/orders/checkout", recorded.Path);
    }

    [Fact]
    public async Task RouteFamilies_ForwardToTheirOwnClustersOnly()
    {
        factory.Identity.Clear();
        factory.Catalog.Clear();
        factory.Order.Clear();
        var client = factory.CreateClient();

        await client.GetAsync("/catalog/api/books");
        await client.GetAsync("/order/api/orders");

        Assert.Single(factory.Catalog.Requests);
        Assert.Single(factory.Order.Requests);
        Assert.Empty(factory.Identity.Requests);
    }

    [Fact]
    public async Task ResponseBody_IsPassedThroughUnchanged()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/catalog/api/books");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("catalog", json.RootElement.GetProperty("cluster").GetString());
        Assert.Equal("/api/books", json.RootElement.GetProperty("path").GetString());
    }

    [Fact]
    public async Task UnmatchedPath_Returns404()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/unknown/route");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PreflightOptions_IsAnsweredByCorsAndNotForwardedUpstream()
    {
        factory.Catalog.Clear();
        var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Options, "/catalog/api/books");
        request.Headers.Add("Origin", "http://localhost:3000");
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("http://localhost:3000",
            response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Empty(factory.Catalog.Requests);
    }

    [Fact]
    public async Task CrossOriginGet_IncludesAllowOriginHeader()
    {
        factory.Catalog.Clear();
        var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/catalog/api/books");
        request.Headers.Add("Origin", "http://localhost:3000");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("http://localhost:3000",
            response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }
}
