using EfCorePills.Data;
using EfCorePills.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace EfCorePills.Pills.CartesianExplosion;

public class CartesianExplosionPill : IPill
{
    public string Name => "cartesian";

    public async Task RunAsync(Measurer measurer)
    {
        measurer.Header("Cartesian explosion (50 orders: 10 lines x 10 payments each)");

        foreach (var split in new[] { false, true })
        {
            var lines = 0;
            var payments = 0;

            await measurer.MeasureAsync(
                split ? "AsSplitQuery" : "AsSingleQuery",
                async () =>
                {
                    await using var db = ShopContextFactory.Create(measurer.Counter);
                    var query = db.Orders.OrderBy(o => o.Id).Take(50)
                        .Include(o => o.Lines)
                        .Include(o => o.Payments)
                        .AsNoTracking();

                    var orders = await (split ? query.AsSplitQuery() : query.AsSingleQuery()).ToListAsync();
                    lines = orders.Sum(o => o.Lines.Count);
                    payments = orders.Sum(o => o.Payments.Count);

                    return orders.Count;
                },
                runs: 5);

            measurer.Note($"{"",34} loaded lines={lines} payments={payments}");
        }

        measurer.Note($"expected rows single query: 50 x 10 x 10 = {50 * 10 * 10}");
    }
}
