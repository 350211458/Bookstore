using Microsoft.EntityFrameworkCore;

namespace Identity.Api.Data;

/// <summary>
/// EF Core data context hosting the OpenIddict stores (applications, authorizations,
/// scopes and tokens) persisted to PostgreSQL.
/// </summary>
public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Register the OpenIddict entity types (applications, authorizations, scopes, tokens)
        // with the default string-keyed entity models.
        modelBuilder.UseOpenIddict();
    }
}
