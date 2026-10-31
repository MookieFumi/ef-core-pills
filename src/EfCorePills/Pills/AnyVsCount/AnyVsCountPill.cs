using System.Linq.Expressions;
using EfCorePills.Data;
using EfCorePills.Diagnostics;
using EfCorePills.Entities;
using Microsoft.EntityFrameworkCore;

namespace EfCorePills.Pills.AnyVsCount;

public class AnyVsCountPill : IPill
{
    public string Name => "any";

    public async Task RunAsync(Measurer measurer)
    {
        measurer.Header("Count() > 0 vs Any()");

        await CompareOnOrdersAsync(measurer, "status = Pending (no index on Status alone)", o => o.Status == "Pending");
        await CompareOnOrdersAsync(measurer, "customer 7 + Pending (index CustomerId,Status)", o => o.CustomerId == 7 && o.Status == "Pending");
        await CompareOnOrdersAsync(measurer, "Total > 5000 (nobody matches)", o => o.Total > 5000);
        await CompareOnShipmentsAsync(measurer);
    }

    private static async Task CompareOnOrdersAsync(Measurer measurer, string label, Expression<Func<Order, bool>> filter)
    {
        measurer.Note($"-- {label}");

        await measurer.MeasureAsync(
            "Count() > 0",
            async () =>
            {
                await using var db = ShopContextFactory.Create(measurer.Counter);
                return await db.Orders.CountAsync(filter) > 0;
            },
            runs: 15);

        await measurer.MeasureAsync(
            "AnyAsync()",
            async () =>
            {
                await using var db = ShopContextFactory.Create(measurer.Counter);
                return await db.Orders.AnyAsync(filter);
            },
            runs: 15);
    }

    private static async Task CompareOnShipmentsAsync(Measurer measurer)
    {
        measurer.Note("-- 1,000,000 shipments, Carrier = 'Carrier 3' (no index, thousands of matches)");

        await measurer.MeasureAsync(
            "Count() > 0",
            async () =>
            {
                await using var db = ShopContextFactory.Create(measurer.Counter);
                return await db.Shipments.CountAsync(s => s.Carrier == "Carrier 3") > 0;
            },
            runs: 7);

        await measurer.MeasureAsync(
            "AnyAsync()",
            async () =>
            {
                await using var db = ShopContextFactory.Create(measurer.Counter);
                return await db.Shipments.AnyAsync(s => s.Carrier == "Carrier 3");
            },
            runs: 7);
    }
}
