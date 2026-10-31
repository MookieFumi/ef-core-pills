using Microsoft.EntityFrameworkCore;

namespace EfCorePills;

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool Active { get; set; }
}

public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; }
    public decimal Total { get; set; }
    // Simulates the "wide" columns of a real table: notes, an attached PDF, etc.
    public string Notes { get; set; } = "";
    public byte[] InvoicePdf { get; set; } = [];
    public List<OrderLine> Lines { get; set; } = [];
    public List<Payment> Payments { get; set; } = [];
}

public class OrderLine
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string Sku { get; set; } = "";
    public int Quantity { get; set; }
}

public class Payment
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
}

public class Shipment
{
    public int Id { get; set; }
    public string Carrier { get; set; } = "";
    public DateTime ShippedAt { get; set; }
}

public class ShopContext(DbContextOptions<ShopContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Shipment> Shipments => Set<Shipment>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Order>().HasIndex(o => new { o.CustomerId, o.Status });
        b.Entity<Shipment>().HasIndex(s => new { s.ShippedAt, s.Id });
        b.Entity<Order>().Property(o => o.Total).HasConversion<double>();
        b.Entity<Payment>().Property(p => p.Amount).HasConversion<double>();
    }
}
