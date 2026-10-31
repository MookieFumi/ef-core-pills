using Microsoft.EntityFrameworkCore;

namespace EfCorePills;

public static class Seed
{
    public const string Path = "pills.db";

    public static ShopContext Create(CommandCounter? counter = null, Action<DbContextOptionsBuilder<ShopContext>>? configure = null)
    {
        var o = new DbContextOptionsBuilder<ShopContext>().UseSqlite($"Data Source={Path}");
        if (counter is not null) o.AddInterceptors(counter);
        configure?.Invoke(o);
        return new ShopContext(o.Options);
    }

    public static void Ensure()
    {
        if (File.Exists(Path)) return;
        using var db = Create();
        db.Database.EnsureCreated();
        var rnd = new Random(42);
        var customers = Enumerable.Range(1, 200).Select(i => new Customer { Name = $"Customer {i}", Active = i % 4 != 0 }).ToList();
        db.Customers.AddRange(customers);
        db.SaveChanges();

        var statuses = new[] { "Pending", "Paid", "Shipped", "Expired" };
        var orders = new List<Order>();
        for (var i = 0; i < 20_000; i++)
        {
            var o = new Order
            {
                CustomerId = customers[rnd.Next(customers.Count)].Id,
                Status = statuses[rnd.Next(statuses.Length)],
                CreatedAt = new DateTime(2026, 1, 1).AddMinutes(i * 7),
                Total = rnd.Next(10, 2000),
                Notes = new string('n', 400),
                InvoicePdf = new byte[20_000],
            };
            for (var l = 0; l < 10; l++) o.Lines.Add(new OrderLine { Sku = $"SKU-{rnd.Next(1000)}", Quantity = rnd.Next(1, 5) });
            for (var p = 0; p < 10; p++) o.Payments.Add(new Payment { Amount = rnd.Next(1, 100) });
            orders.Add(o);
            if (orders.Count == 1000) { db.Orders.AddRange(orders); db.SaveChanges(); db.ChangeTracker.Clear(); orders.Clear(); }
        }
        // Shipments: many share the same timestamp, so ordering by date alone is NOT unique.
        var ship = Enumerable.Range(0, 1_000_000).Select(i => new Shipment
        {
            Carrier = $"Carrier {i % 7}",
            ShippedAt = new DateTime(2026, 1, 1).AddMinutes(i / 3),
        });
        foreach (var chunk in ship.Chunk(10_000)) { db.Shipments.AddRange(chunk); db.SaveChanges(); db.ChangeTracker.Clear(); }
    }
}
