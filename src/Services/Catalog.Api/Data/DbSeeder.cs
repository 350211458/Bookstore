using Catalog.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Data;

/// <summary>
/// Idempotent development seeding: creates the schema (if missing) and inserts a small set
/// of books (spec 03: "Seed a small set of books idempotently on startup").
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var dbContext = services.GetRequiredService<CatalogDbContext>();

        // Create the schema on startup for the scaffold. Replace with EF Core migrations
        // when moving to a real deployment.
        await dbContext.Database.EnsureCreatedAsync();

        if (await dbContext.Books.AnyAsync())
        {
            return;
        }

        var now = DateTime.UtcNow;
        dbContext.Books.AddRange(
            new Book
            {
                Title = "Domain-Driven Design",
                Author = "Eric Evans",
                ISBN = "978-0321125217",
                Price = 45.00m,
                StockQuantity = 50,
                Category = "Software Engineering",
                CreatedAt = now,
                UpdatedAt = now,
            },
            new Book
            {
                Title = "Clean Code",
                Author = "Robert C. Martin",
                ISBN = "978-0132350884",
                Price = 38.00m,
                StockQuantity = 30,
                Category = "Software Engineering",
                CreatedAt = now,
                UpdatedAt = now,
            },
            new Book
            {
                Title = "Design Patterns",
                Author = "Erich Gamma",
                ISBN = "978-0201633610",
                Price = 52.00m,
                StockQuantity = 20,
                Category = "Software Engineering",
                CreatedAt = now,
                UpdatedAt = now,
            },
            new Book
            {
                Title = "The Hobbit",
                Author = "J.R.R. Tolkien",
                ISBN = "978-0547928227",
                Price = 18.00m,
                StockQuantity = 100,
                Category = "Fantasy",
                CreatedAt = now,
                UpdatedAt = now,
            });

        await dbContext.SaveChangesAsync();
    }
}
