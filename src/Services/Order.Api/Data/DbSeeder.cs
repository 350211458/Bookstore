using Microsoft.EntityFrameworkCore;

namespace Order.Api.Data;

/// <summary>
/// Ensures the schema exists on startup (spec 04: "Ensure schema on startup (EnsureCreated);
/// no seed data required").
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var dbContext = services.GetRequiredService<OrderDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }
}
