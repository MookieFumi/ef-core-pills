using EfCorePills.Entities;
using Microsoft.EntityFrameworkCore;

namespace EfCorePills.Data;

public class ShopContext(DbContextOptions<ShopContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<Shipment> Shipments => Set<Shipment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>().HasIndex(o => new { o.CustomerId, o.Status });
        modelBuilder.Entity<Shipment>().HasIndex(s => new { s.ShippedAt, s.Id });
        modelBuilder.Entity<Order>().Property(o => o.Total).HasConversion<double>();
        modelBuilder.Entity<Payment>().Property(p => p.Amount).HasConversion<double>();
    }
}
