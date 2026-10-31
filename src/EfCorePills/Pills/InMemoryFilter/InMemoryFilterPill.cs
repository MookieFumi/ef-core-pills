using EfCorePills.Data;
using EfCorePills.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace EfCorePills.Pills.InMemoryFilter;

public class InMemoryFilterPill : IPill
{
    public string Name => "in-memory";

    public async Task RunAsync(Measurer measurer)
    {
        measurer.Header("Filtering in memory (pending orders of customer 7)");

        await measurer.MeasureAsync("ToList().Where(...)", () => FilterAfterToListAsync(measurer), runs: 3);
        await measurer.MeasureAsync("Where(...).ToList()", () => FilterInDatabaseAsync(measurer), runs: 3);
    }

    private static async Task<int> FilterAfterToListAsync(Measurer measurer)
    {
        await using var db = ShopContextFactory.Create(measurer.Counter);
        var all = await db.Orders.AsNoTracking().ToListAsync();

        return all.Where(o => o.Status == "Pending" && o.CustomerId == 7).ToList().Count;
    }

    private static async Task<int> FilterInDatabaseAsync(Measurer measurer)
    {
        await using var db = ShopContextFactory.Create(measurer.Counter);
        var pending = await db.Orders.AsNoTracking()
            .Where(o => o.Status == "Pending" && o.CustomerId == 7)
            .ToListAsync();

        return pending.Count;
    }
}
