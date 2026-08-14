using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;

namespace Identity.Api.Data;

/// <summary>
/// Idempotent development seeding: creates the database schema (if missing) and registers
/// the OpenIddict applications required by the password and client-credentials flows.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        // Create the schema on startup for the scaffold. Replace with EF Core migrations
        // when moving to a real deployment.
        var dbContext = services.GetRequiredService<IdentityDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        var applications = services.GetRequiredService<IOpenIddictApplicationManager>();
        var scopes = services.GetRequiredService<IOpenIddictScopeManager>();

        // Register the scopes the password flow can request. The standard OIDC scopes
        // (profile, email, ...) are known to OpenIddict; "roles" is a custom scope and must
        // be registered before it can be requested.
        foreach (var (name, displayName) in new[]
                 {
                     ("profile", "Basic profile information"),
                     ("email", "Email address"),
                     ("roles", "User roles"),
                 })
        {
            if (await scopes.FindByNameAsync(name) is null)
            {
                await scopes.CreateAsync(new OpenIddictScopeDescriptor
                {
                    Name = name,
                    DisplayName = displayName,
                });
            }
        }

        // Public client used by the interactive/password flows. No client secret is set.
        if (await applications.FindByClientIdAsync("bookstore-app") is null)
        {
            await applications.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "bookstore-app",
                DisplayName = "Bookstore Client Application",
                Permissions =
                {
                    OpenIddictConstants.Permissions.Endpoints.Token,
                    OpenIddictConstants.Permissions.Endpoints.Authorization,

                    OpenIddictConstants.Permissions.GrantTypes.Password,
                    OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                    OpenIddictConstants.Permissions.GrantTypes.ClientCredentials,

                    OpenIddictConstants.Permissions.Scopes.Email,
                    OpenIddictConstants.Permissions.Scopes.Profile,
                    OpenIddictConstants.Permissions.Scopes.Roles,
                },
                Requirements =
                {
                    OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange,
                },
            });
        }

        // Confidential client used by machine-to-machine (client credentials) calls.
        // NOTE: production deployments must store a hashed client secret (e.g. via
        // IPasswordHasher<OpenIddictApplication>) instead of the plaintext below.
        if (await applications.FindByClientIdAsync("bookstore-service") is null)
        {
            await applications.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "bookstore-service",
                ClientSecret = "bookstore-secret",
                DisplayName = "Bookstore Service (machine-to-machine)",
                Permissions =
                {
                    OpenIddictConstants.Permissions.Endpoints.Token,
                    OpenIddictConstants.Permissions.GrantTypes.ClientCredentials,
                    OpenIddictConstants.Permissions.Scopes.Roles,
                },
            });
        }
    }
}
