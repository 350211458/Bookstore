using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Order.Api.Services;

namespace Order.Api.Tests;

/// <summary>
/// Boots Order.Api against a file-backed SQLite database (Database:Provider=sqlite)
/// and substitutes IStockDeductionService with a configurable stub (spec 04) so
/// checkout tests never dial out to a real catalog service.
/// </summary>
public sealed class OrderApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(
        Path.GetTempPath(), $"order-tests-{Guid.NewGuid():N}.db");

    /// <summary>Stub registered in place of the real gRPC DeductStock client.</summary>
    public StubStockDeductionService StockDeduction { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "sqlite",
                ["ConnectionStrings:OrderDbSqlite"] = $"Data Source={_dbPath}",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IStockDeductionService>(StockDeduction);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && File.Exists(_dbPath))
        {
            try
            {
                File.Delete(_dbPath);
            }
            catch (IOException)
            {
                // The SQLite file may still be locked briefly; leftover temp files are harmless.
            }
        }
    }
}
