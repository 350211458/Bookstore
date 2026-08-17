using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Order.Api.Data;
using Order.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add shared OpenTelemetry, resilience, health checks and service discovery.
builder.AddServiceDefaults();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// --- EF Core: PostgreSQL hosting the order data ---
// The provider is selectable via configuration so integration tests can swap in SQLite
// (Database:Provider=sqlite) without registering two providers in the same container.
builder.Services.AddDbContext<OrderDbContext>((_, options) =>
{
    var useSqlite = string.Equals(
        builder.Configuration["Database:Provider"], "sqlite", StringComparison.OrdinalIgnoreCase);

    var connectionString = builder.Configuration.GetConnectionString(useSqlite ? "OrderDbSqlite" : "OrderDb")
        ?? (useSqlite ? "Data Source=order.db"
                      : "Host=localhost;Port=5432;Database=bookstore_order;Username=postgres;Password=postgres");

    if (useSqlite)
    {
        options.UseSqlite(connectionString);
    }
    else
    {
        options.UseNpgsql(connectionString);
    }
});

// --- gRPC client to Catalog.Api's DeductStock, wrapped behind a testable seam (spec 04) ---
// The channel address comes from configuration (e.g. injected by .NET Aspire in AppHost).
// Integration tests replace this registration with a stub.
builder.Services.AddSingleton<IStockDeductionService>(_ =>
{
    var endpoint = builder.Configuration["Catalog:GrpcEndpoint"]
        ?? throw new InvalidOperationException("Catalog:GrpcEndpoint is not configured.");
    return new CatalogStockDeductionService(GrpcChannel.ForAddress(endpoint));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// TLS is terminated at the API gateway (YARP), so no HTTPS redirection per service.
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapControllers();

// Ensure the order schema exists on startup (no seed data required).
using (var scope = app.Services.CreateScope())
{
    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

app.Run();

// Expose the generated entry point so WebApplicationFactory<Program> can bootstrap the app in tests.
public partial class Program;
