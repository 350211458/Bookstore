using Catalog.Api.Data;
using Catalog.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add shared OpenTelemetry, resilience, health checks and service discovery.
builder.AddServiceDefaults();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// --- EF Core: PostgreSQL hosting the catalog data ---
// The provider is selectable via configuration so integration tests can swap in SQLite
// (Database:Provider=sqlite) without registering two providers in the same container.
// The switch is read lazily (when the DbContext options are built) so configuration
// overrides applied by WebApplicationFactory during host building are honored.
builder.Services.AddDbContext<CatalogDbContext>((_, options) =>
{
    var useSqlite = string.Equals(
        builder.Configuration["Database:Provider"], "sqlite", StringComparison.OrdinalIgnoreCase);

    var connectionString = builder.Configuration.GetConnectionString(useSqlite ? "CatalogDbSqlite" : "CatalogDb")
        ?? (useSqlite ? "Data Source=catalog.db"
                      : "Host=localhost;Port=5432;Database=bookstore_catalog;Username=postgres;Password=postgres");

    if (useSqlite)
    {
        options.UseSqlite(connectionString);
    }
    else
    {
        options.UseNpgsql(connectionString);
    }
});

// --- gRPC: DeductStock stock-deduction endpoint (spec 03) ---
builder.Services.AddGrpc();
builder.Services.AddScoped<StockService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// TLS is terminated at the API gateway (YARP), so no HTTPS redirection per service.
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapControllers();
app.MapGrpcService<CatalogGrpcService>();

// Seed the catalog schema and books on startup (idempotent).
using (var scope = app.Services.CreateScope())
{
    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

app.Run();

// Expose the generated entry point so WebApplicationFactory<Program> can bootstrap the app in tests.
public partial class Program;
