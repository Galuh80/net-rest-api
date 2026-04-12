using Microsoft.EntityFrameworkCore;
using RestAPI.Models;

namespace RestAPI.Infrastructures;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Unique constraint on User.Email
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // Index on Product.CategoryId for faster queries
        modelBuilder.Entity<Product>()
            .HasIndex(p => p.CategoryId);

        // RefreshToken indexes
        modelBuilder.Entity<RefreshToken>()
            .HasIndex(r => r.Token)
            .IsUnique();

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(r => r.UserId);
    }
}
