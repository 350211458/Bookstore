using Identity.Api.Data;
using Identity.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add shared OpenTelemetry, resilience, health checks and service discovery.
builder.AddServiceDefaults();

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddAuthorization();

// Development-only in-memory user store used by the password grant.
builder.Services.AddSingleton<InMemoryUserStore>();

// --- EF Core: PostgreSQL hosting the OpenIddict stores ---
// The provider is selectable via configuration so integration tests can swap in SQLite
// (Database:Provider=sqlite) without registering two providers in the same container.
// The switch is read lazily (when the DbContext options are built) so configuration
// overrides applied by WebApplicationFactory during host building are honored.
builder.Services.AddDbContext<IdentityDbContext>((_, options) =>
{
    var useSqlite = string.Equals(
        builder.Configuration["Database:Provider"], "sqlite", StringComparison.OrdinalIgnoreCase);

    var connectionString = builder.Configuration.GetConnectionString(useSqlite ? "IdentityDbSqlite" : "IdentityDb")
        ?? (useSqlite ? "Data Source=identity.db"
                      : "Host=localhost;Port=5432;Database=bookstore_identity;Username=postgres;Password=postgres");

    if (useSqlite)
    {
        options.UseSqlite(connectionString);
    }
    else
    {
        options.UseNpgsql(connectionString);
    }
});

// --- OpenIddict: OAuth 2.0 / OpenID Connect server ---
builder.Services.AddOpenIddict()
    .AddCore(options =>
        options.UseEntityFrameworkCore()
               .UseDbContext<IdentityDbContext>())
    .AddServer(options =>
    {
        // Register the authorization, token and userinfo endpoints.
        options.SetAuthorizationEndpointUris("/connect/authorize");
        options.SetTokenEndpointUris("/connect/token");
        options.SetUserInfoEndpointUris("/connect/userinfo");

        // Enable the authorization code, password and client credentials flows.
        options.AllowAuthorizationCodeFlow();
        options.AllowPasswordFlow();
        options.AllowClientCredentialsFlow();

        // Development signing and encryption credentials (ephemeral, generated on startup).
        options.AddDevelopmentSigningCertificate();
        options.AddDevelopmentEncryptionCertificate();

        // Emit readable JWT access tokens (signed, not encrypted) so clients can inspect claims.
        options.DisableAccessTokenEncryption();

        // Register the ASP.NET Core host and pass token/authorization/userinfo requests through
        // to the controllers in this project. HTTPS is not required inside the mesh because
        // TLS is terminated at the API gateway (YARP) in front of the services.
        options.UseAspNetCore()
               .DisableTransportSecurityRequirement()
               .EnableAuthorizationEndpointPassthrough()
               .EnableTokenEndpointPassthrough()
               .EnableUserInfoEndpointPassthrough();
    })
    .AddValidation(options =>
    {
        // Validate tokens locally against the server's signing keys.
        options.UseLocalServer();
        options.UseAspNetCore();
    });

// Enable the OpenIddict validation handler as the default authentication scheme so that
// the userinfo endpoint (and any downstream API) can validate presented access tokens.
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// TLS is terminated at the API gateway (YARP), so no HTTPS redirection per service.
app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapControllers();

// Seed the OpenIddict applications and schema on startup (idempotent).
using (var scope = app.Services.CreateScope())
{
    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

app.Run();

// Expose the generated entry point so WebApplicationFactory<Program> can bootstrap the app in tests.
public partial class Program;
