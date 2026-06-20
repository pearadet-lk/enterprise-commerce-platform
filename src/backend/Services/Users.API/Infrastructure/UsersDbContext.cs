using Microsoft.EntityFrameworkCore;
using Users.API.Domain;

namespace Users.API.Infrastructure;

public class UsersDbContext(DbContextOptions<UsersDbContext> options) : DbContext(options)
{
    public DbSet<UserProfile> Users => Set<UserProfile>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.UserName).IsUnique();
        });
    }
}

public static class UsersSeed
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
        await db.Database.EnsureCreatedAsync();

        if (await db.Users.AnyAsync())
        {
            return;
        }

        db.Users.AddRange(
            new UserProfile
            {
                Email = "admin@enterprise.local",
                UserName = "admin",
                FullName = "Platform Admin",
                Roles = "Admin",
                CreatedBy = "system"
            },
            new UserProfile
            {
                Email = "manager@enterprise.local",
                UserName = "manager",
                FullName = "Order Manager",
                Roles = "Manager",
                CreatedBy = "system"
            },
            new UserProfile
            {
                Email = "user@enterprise.local",
                UserName = "user",
                FullName = "Standard User",
                Roles = "User",
                CreatedBy = "system"
            });

        db.AuditLogs.AddRange(
            new AuditLog
            {
                Action = "Seed",
                EntityType = "System",
                UserId = "system",
                UserName = "system",
                Details = "Initial platform seed completed."
            },
            new AuditLog
            {
                Action = "Login",
                EntityType = "User",
                EntityId = "admin",
                UserId = "admin",
                UserName = "admin",
                Details = "Admin user signed in during bootstrap."
            });

        await db.SaveChangesAsync();
    }
}
