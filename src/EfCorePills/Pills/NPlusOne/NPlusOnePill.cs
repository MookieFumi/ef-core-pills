using EfCorePills.Data;
using EfCorePills.Diagnostics;
using EfCorePills.Entities;
using Microsoft.EntityFrameworkCore;

namespace EfCorePills.Pills.NPlusOne;

public class NPlusOnePill : IPill
{
    public string Name => "n-plus-one";

    public async Task RunAsync(Measurer measurer)
    {
        measurer.Header("N+1 (first 300 orders and their lines)");

        await measurer.MeasureAsync("one query per order", () => QueryPerOrderAsync(measurer), runs: 5);
        await measurer.MeasureAsync("Include", () => WithIncludeAsync(measurer), runs: 5);
        await measurer.MeasureAsync("projection", () => WithProjectionAsync(measurer), runs: 5);
    }

    private static async Task<int> QueryPerOrderAsync(Measurer measurer)
    {
        // Same effect as touching a lazy-loaded navigation inside the loop.
        await using var db = ShopContextFactory.Create(measurer.Counter);
        var orders = await db.Orders.OrderBy(o => o.Id).Take(300).ToListAsync();
        var lines = 0;

        foreach (var order in orders)
        {
            lines += await db.Set<OrderLine>().CountAsync(l => l.OrderId == order.Id);
        }

        return lines;
    }

    private static async Task<int> WithIncludeAsync(Measurer measurer)
    {
        await using var db = ShopContextFactory.Create(measurer.Counter);
        var orders = await db.Orders.OrderBy(o => o.Id).Take(300).Include(o => o.Lines).ToListAsync();

        return orders.Sum(o => o.Lines.Count);
    }

    private static async Task<int> WithProjectionAsync(Measurer measurer)
    {
        await using var db = ShopContextFactory.Create(measurer.Counter);
        var rows = await db.Orders.OrderBy(o => o.Id).Take(300)
            .Select(o => new { o.Id, LineCount = o.Lines.Count })
            .ToListAsync();

        return rows.Sum(r => r.LineCount);
    }
}
