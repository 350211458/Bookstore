using Microsoft.EntityFrameworkCore;
using Order.Api.Models;
// The entity type Order collides with the Order namespace segment of the project's root
// namespace (Order.Api.*), so reference it through an explicit alias.
using OrderEntity = Order.Api.Models.Order;

namespace Order.Api.Data;

/// <summary>
/// EF Core context for the order service (spec 04).
/// </summary>
public sealed class OrderDbContext(DbContextOptions<OrderDbContext> options) : DbContext(options)
{
    public DbSet<CartItem> CartItems => Set<CartItem>();

    public DbSet<OrderEntity> Orders => Set<OrderEntity>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Cart lookups are always by SessionId.
        modelBuilder.Entity<CartItem>().HasIndex(c => c.SessionId);

        // OrderItem.OrderId references Order.Id (no navigation properties, so the
        // relationship is configured from the scalar FK only — no entity fields added).
        modelBuilder.Entity<OrderItem>()
            .HasOne<OrderEntity>()
            .WithMany()
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
