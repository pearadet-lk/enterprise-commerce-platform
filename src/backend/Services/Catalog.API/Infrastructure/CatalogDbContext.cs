using Catalog.API.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catalog.API.Infrastructure;

public class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasIndex(p => p.Sku).IsUnique();
            entity.Property(p => p.UnitPrice).HasPrecision(18, 2);
        });
    }
}

public static class CatalogSeed
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        await db.Database.EnsureCreatedAsync();

        if (await db.Products.AnyAsync())
        {
            return;
        }

        db.Products.AddRange(
            new Product
            {
                Sku = "SKU-001",
                Name = "Industrial Widget A",
                Description = "Heavy-duty widget for manufacturing lines.",
                UnitPrice = 49.99m,
                StockQuantity = 250,
                CreatedBy = "system"
            },
            new Product
            {
                Sku = "SKU-002",
                Name = "Precision Gear Set",
                Description = "High-tolerance gear assembly.",
                UnitPrice = 129.50m,
                StockQuantity = 80,
                CreatedBy = "system"
            },
            new Product
            {
                Sku = "SKU-003",
                Name = "Safety Valve Kit",
                Description = "Replacement safety valve kit.",
                UnitPrice = 89.00m,
                StockQuantity = 120,
                CreatedBy = "system"
            });

        await db.SaveChangesAsync();
    }
}
