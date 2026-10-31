using EfCorePills.Data;
using EfCorePills.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace EfCorePills.Pills.Tracking;

public class TrackingPill : IPill
{
    public string Name => "tracking";

    public async Task RunAsync(Measurer measurer)
    {
        await MeasureTrackingAsync(measurer);
        await ShowIdentityResolutionAsync();
    }

    private static async Task MeasureTrackingAsync(Measurer measurer)
    {
        measurer.Header("Tracking (50,000 narrow rows: shipments)");

        foreach (var tracking in new[] { true, false })
        {
            var tracked = 0;
            long allocatedKb = 0;
            var label = tracking ? "tracking" : "AsNoTracking";

            await measurer.MeasureAsync(
                label,
                async () =>
                {
                    await using var db = ShopContextFactory.Create(measurer.Counter);
                    var before = GC.GetAllocatedBytesForCurrentThread();
                    var query = db.Shipments.OrderBy(s => s.Id).Take(50_000);
                    var shipments = tracking ? await query.ToListAsync() : await query.AsNoTracking().ToListAsync();

                    allocatedKb = (GC.GetAllocatedBytesForCurrentThread() - before) / 1024;
                    tracked = db.ChangeTracker.Entries().Count();

                    return shipments.Count;
                },
                runs: 7);

            measurer.Note($"{"",34} tracked entities={tracked,6}  allocated={allocatedKb,7} KB");
        }
    }

    private static async Task ShowIdentityResolutionAsync()
    {
        Console.WriteLine();
        Console.WriteLine("=== Identity resolution (500 orders, 200 customers) ===");

        foreach (var mode in new[] { "tracking", "AsNoTracking", "AsNoTrackingWithIdentityResolution" })
        {
            await using var db = ShopContextFactory.Create();
            var query = db.Orders.Include(o => o.Customer).OrderBy(o => o.Id).Take(500);

            query = mode switch
            {
                "AsNoTracking" => query.AsNoTracking(),
                "AsNoTrackingWithIdentityResolution" => query.AsNoTrackingWithIdentityResolution(),
                _ => query,
            };

            var orders = await query.ToListAsync();
            var customers = orders.Select(o => o.Customer).Distinct().Count();

            Console.WriteLine($"{mode,-36} orders={orders.Count} distinct Customer instances={customers}");
        }
    }
}
