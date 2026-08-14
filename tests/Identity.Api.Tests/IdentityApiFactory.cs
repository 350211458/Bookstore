using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Identity.Api.Tests;

/// <summary>
/// Boots the Identity.Api web application against a file-backed SQLite database instead of
/// PostgreSQL (via the <c>Database:Provider=sqlite</c> switch in Program.cs), so integration
/// tests can exercise the real HTTP pipeline without a database server.
/// </summary>
public sealed class IdentityApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(
        Path.GetTempPath(), $"identity-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "sqlite",
                ["ConnectionStrings:IdentityDbSqlite"] = $"Data Source={_dbPath}",
            });
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
