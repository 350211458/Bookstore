using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ApiGateway.Tests;

/// <summary>
/// Boots the YARP gateway (spec 05) with the three cluster destinations redirected to
/// in-test stub upstreams, so routing and prefix stripping are verified without the real
/// services. Service discovery resolves the literal 127.0.0.1 addresses without DNS.
/// </summary>
public sealed class ApiGatewayFactory : WebApplicationFactory<Program>
{
    public StubUpstream Identity { get; } = StubUpstream.Start("identity");
    public StubUpstream Catalog { get; } = StubUpstream.Start("catalog");
    public StubUpstream Order { get; } = StubUpstream.Start("order");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ReverseProxy:Clusters:identity:Destinations:identity-api:Address"] = Identity.BaseUrl,
                ["ReverseProxy:Clusters:catalog:Destinations:catalog-api:Address"] = Catalog.BaseUrl,
                ["ReverseProxy:Clusters:order:Destinations:order-api:Address"] = Order.BaseUrl,
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            Identity.Dispose();
            Catalog.Dispose();
            Order.Dispose();
        }
    }
}
