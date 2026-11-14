using EfCoreInterceptors.Entities;
using Microsoft.EntityFrameworkCore;

namespace EfCoreInterceptors.Data;

public class ShopContext(DbContextOptions<ShopContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>().Property(p => p.Price).HasConversion<double>();
        modelBuilder.Entity<Product>().HasIndex(p => p.Name).IsUnique();
        modelBuilder.Entity<Product>().HasQueryFilter(p => !p.IsDeleted);
    }
}
