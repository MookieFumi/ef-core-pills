using EfCorePills.Data;
using EfCorePills.Diagnostics;
using EfCorePills.Entities;
using Microsoft.EntityFrameworkCore;

namespace EfCorePills.Pills.Pagination;

public class PaginationPill : IPill
{
    private const int PageSize = 25;
    private const int Runs = 15;

    private static readonly int[] Pages = [1, 1000, 20000, 40000];

    public string Name => "paging";

    public async Task RunAsync(Measurer measurer)
    {
        measurer.Header("Pagination over 1,000,000 shipments");

        foreach (var page in Pages)
        {
            await measurer.MeasureAsync(
                $"offset, page {page} (size {PageSize})",
                () => OffsetPageAsync(measurer, page),
                runs: Runs);
        }

        foreach (var page in Pages)
        {
            var last = await FindLastRowOfPreviousPageAsync(page);

            await measurer.MeasureAsync(
                $"keyset, page {page} (size {PageSize})",
                () => KeysetPageAsync(measurer, last),
                runs: Runs);
        }
    }

    private static async Task<int> OffsetPageAsync(Measurer measurer, int page)
    {
        await using var db = ShopContextFactory.Create(measurer.Counter);
        var shipments = await db.Shipments.AsNoTracking()
            .OrderBy(s => s.ShippedAt).ThenBy(s => s.Id)
            .Skip((page - 1) * PageSize).Take(PageSize)
            .ToListAsync();

        return shipments.Count;
    }

    // The cursor the client would hold between requests.
    private static async Task<Shipment?> FindLastRowOfPreviousPageAsync(int page)
    {
        if (page == 1)
        {
            return null;
        }

        await using var db = ShopContextFactory.Create();

        return await db.Shipments.AsNoTracking()
            .OrderBy(s => s.ShippedAt).ThenBy(s => s.Id)
            .Skip((page - 1) * PageSize - 1).Take(1)
            .FirstAsync();
    }

    private static async Task<int> KeysetPageAsync(Measurer measurer, Shipment? last)
    {
        await using var db = ShopContextFactory.Create(measurer.Counter);
        var query = db.Shipments.AsNoTracking().OrderBy(s => s.ShippedAt).ThenBy(s => s.Id).AsQueryable();

        if (last is not null)
        {
            query = query.Where(s => s.ShippedAt > last.ShippedAt
                || (s.ShippedAt == last.ShippedAt && s.Id > last.Id));
        }

        var shipments = await query.Take(PageSize).ToListAsync();

        return shipments.Count;
    }
}
