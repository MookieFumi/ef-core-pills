using EfCorePills.Data;
using EfCorePills.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace EfCorePills.Pills.Projection;

public class ProjectionPill : IPill
{
    public string Name => "projection";

    public async Task RunAsync(Measurer measurer)
    {
        measurer.Header("Whole entity vs projection (2,000 orders)");

        await measurer.MeasureAsync("whole entity", () => WholeEntityAsync(measurer));
        measurer.Note(measurer.Counter.Sql[0]);

        await measurer.MeasureAsync("Select to record", () => ProjectionAsync(measurer));
        measurer.Note(measurer.Counter.Sql[0]);
    }

    private static async Task<int> WholeEntityAsync(Measurer measurer)
    {
        await using var db = ShopContextFactory.Create(measurer.Counter);
        var orders = await db.Orders.AsNoTracking().OrderBy(o => o.Id).Take(2000).ToListAsync();

        return orders.Count;
    }

    private static async Task<int> ProjectionAsync(Measurer measurer)
    {
        await using var db = ShopContextFactory.Create(measurer.Counter);
        var rows = await db.Orders.AsNoTracking().OrderBy(o => o.Id).Take(2000)
            .Select(o => new OrderRow(o.Id, o.Status, o.Total))
            .ToListAsync();

        return rows.Count;
    }
}
