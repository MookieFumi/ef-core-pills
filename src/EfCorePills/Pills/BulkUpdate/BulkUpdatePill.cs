using EfCorePills.Data;
using EfCorePills.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace EfCorePills.Pills.BulkUpdate;

public class BulkUpdatePill : IPill
{
    public string Name => "bulk";

    public async Task RunAsync(Measurer measurer)
    {
        measurer.Header("SaveChanges in a loop vs ExecuteUpdate (expire 2,000 pending orders)");

        var ids = await FindPendingOrderIdsAsync();

        await measurer.MeasureAsync("SaveChanges per entity", () => SaveChangesPerEntityAsync(measurer, ids));
        await ResetStatusAsync(ids);

        await measurer.MeasureAsync("one SaveChanges", () => SingleSaveChangesAsync(measurer, ids));
        await ResetStatusAsync(ids);

        await measurer.MeasureAsync("ExecuteUpdate", () => ExecuteUpdateAsync(measurer, ids));
        await ResetStatusAsync(ids);
    }

    private static async Task<List<int>> FindPendingOrderIdsAsync()
    {
        await using var db = ShopContextFactory.Create();

        return await db.Orders
            .Where(o => o.Status == "Pending")
            .OrderBy(o => o.Id)
            .Take(2000)
            .Select(o => o.Id)
            .ToListAsync();
    }

    // Entities are loaded once; the loop calls SaveChanges on every iteration.
    private static async Task<int> SaveChangesPerEntityAsync(Measurer measurer, List<int> ids)
    {
        await using var db = ShopContextFactory.Create(measurer.Counter);
        var orders = await db.Orders.Where(o => ids.Contains(o.Id)).ToListAsync();

        foreach (var order in orders)
        {
            order.Status = "Expired";
            await db.SaveChangesAsync();
        }

        return orders.Count;
    }

    private static async Task<int> SingleSaveChangesAsync(Measurer measurer, List<int> ids)
    {
        await using var db = ShopContextFactory.Create(measurer.Counter);
        var orders = await db.Orders.Where(o => ids.Contains(o.Id)).ToListAsync();

        foreach (var order in orders)
        {
            order.Status = "Expired";
        }

        await db.SaveChangesAsync();

        return orders.Count;
    }

    private static async Task<int> ExecuteUpdateAsync(Measurer measurer, List<int> ids)
    {
        await using var db = ShopContextFactory.Create(measurer.Counter);

        return await db.Orders
            .Where(o => ids.Contains(o.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, "Expired"));
    }

    private static async Task ResetStatusAsync(List<int> ids)
    {
        await using var db = ShopContextFactory.Create();

        await db.Orders
            .Where(o => ids.Contains(o.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, "Pending"));
    }
}
