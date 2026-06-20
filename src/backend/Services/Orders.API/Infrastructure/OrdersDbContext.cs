using Orders.API.Domain;
using Microsoft.EntityFrameworkCore;

namespace Orders.API.Infrastructure;

public class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasIndex(o => o.OrderNumber).IsUnique();
            entity.Property(o => o.TotalAmount).HasPrecision(18, 2);
            entity.HasMany(o => o.Lines).WithOne(l => l.Order).HasForeignKey(l => l.OrderId);
        });

        modelBuilder.Entity<OrderLine>(entity =>
        {
            entity.Property(l => l.UnitPrice).HasPrecision(18, 2);
        });
    }
}

public static class OrdersSeed
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        await db.Database.EnsureCreatedAsync();
    }
}
