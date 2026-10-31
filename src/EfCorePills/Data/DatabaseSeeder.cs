using EfCorePills.Entities;

namespace EfCorePills.Data;

public static class DatabaseSeeder
{
    private const int CustomerCount = 200;
    private const int OrderCount = 20_000;
    private const int ShipmentCount = 1_000_000;

    public static void EnsureSeeded()
    {
        if (File.Exists(ShopContextFactory.DatabasePath))
        {
            return;
        }

        using var db = ShopContextFactory.Create();
        db.Database.EnsureCreated();

        var random = new Random(42);
        var customers = SeedCustomers(db);
        SeedOrders(db, random, customers);
        SeedShipments(db);
    }

    private static List<Customer> SeedCustomers(ShopContext db)
    {
        var customers = Enumerable.Range(1, CustomerCount)
            .Select(i => new Customer { Name = $"Customer {i}", Active = i % 4 != 0 })
            .ToList();

        db.Customers.AddRange(customers);
        db.SaveChanges();

        return customers;
    }

    private static void SeedOrders(ShopContext db, Random random, List<Customer> customers)
    {
        var statuses = new[] { "Pending", "Paid", "Shipped", "Expired" };
        var batch = new List<Order>();

        for (var i = 0; i < OrderCount; i++)
        {
            var order = new Order
            {
                CustomerId = customers[random.Next(customers.Count)].Id,
                Status = statuses[random.Next(statuses.Length)],
                CreatedAt = new DateTime(2026, 1, 1).AddMinutes(i * 7),
                Total = random.Next(10, 2000),
                Notes = new string('n', 400),
                InvoicePdf = new byte[20_000],
            };

            for (var line = 0; line < 10; line++)
            {
                order.Lines.Add(new OrderLine { Sku = $"SKU-{random.Next(1000)}", Quantity = random.Next(1, 5) });
            }

            for (var payment = 0; payment < 10; payment++)
            {
                order.Payments.Add(new Payment { Amount = random.Next(1, 100) });
            }

            batch.Add(order);

            if (batch.Count == 1000)
            {
                SaveBatch(db, batch);
            }
        }
    }

    // Many shipments share the same timestamp (three per minute), so ordering by date alone is not unique.
    private static void SeedShipments(ShopContext db)
    {
        var shipments = Enumerable.Range(0, ShipmentCount).Select(i => new Shipment
        {
            Carrier = $"Carrier {i % 7}",
            ShippedAt = new DateTime(2026, 1, 1).AddMinutes(i / 3),
        });

        foreach (var chunk in shipments.Chunk(10_000))
        {
            db.Shipments.AddRange(chunk);
            db.SaveChanges();
            db.ChangeTracker.Clear();
        }
    }

    private static void SaveBatch(ShopContext db, List<Order> batch)
    {
        db.Orders.AddRange(batch);
        db.SaveChanges();
        db.ChangeTracker.Clear();
        batch.Clear();
    }
}
