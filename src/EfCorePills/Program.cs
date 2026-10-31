using System.Diagnostics;
using EfCorePills;
using Microsoft.EntityFrameworkCore;

Seed.Ensure();
var pill = args.FirstOrDefault() ?? "all";
var counter = new CommandCounter();

void Header(string t) => Console.WriteLine($"\n=== {t} ===");
void Report(string label, Stopwatch sw) =>
    Console.WriteLine($"{label,-34} commands={counter.Count,6}  rows={counter.Rows,8}  time={sw.ElapsedMilliseconds,6} ms");

async Task<T> Measure<T>(string label, Func<Task<T>> run, int runs = 1)
{
    // With runs > 1: one warm-up run, then the median of the measured runs.
    if (runs > 1) await run();
    var times = new List<long>();
    T r = default!;
    for (var i = 0; i < runs; i++)
    {
        counter.Reset();
        var sw = Stopwatch.StartNew();
        r = await run();
        sw.Stop();
        times.Add(sw.ElapsedMilliseconds);
    }
    times.Sort();
    Console.WriteLine($"{label,-34} commands={counter.Count,6}  rows={counter.Rows,8}  median={times[times.Count / 2],6} ms (n={runs})");
    return r;
}

if (pill is "n-plus-one" or "all")
{
    Header("N+1 (first 300 orders and their lines)");
    await Measure("one query per order", async () =>
    {
        await using var db = Seed.Create(counter);
        var orders = await db.Orders.OrderBy(o => o.Id).Take(300).ToListAsync();
        var n = 0;
        foreach (var o in orders)
            n += await db.Set<OrderLine>().CountAsync(l => l.OrderId == o.Id); // same effect as lazy loading
        return n;
    }, runs: 5);
    await Measure("Include", async () =>
    {
        await using var db = Seed.Create(counter);
        var orders = await db.Orders.OrderBy(o => o.Id).Take(300).Include(o => o.Lines).ToListAsync();
        return orders.Sum(o => o.Lines.Count);
    }, runs: 5);
    await Measure("projection", async () =>
    {
        await using var db = Seed.Create(counter);
        var rows = await db.Orders.OrderBy(o => o.Id).Take(300)
            .Select(o => new { o.Id, LineCount = o.Lines.Count }).ToListAsync();
        return rows.Sum(r => r.LineCount);
    }, runs: 5);
}

if (pill is "tracking" or "all")
{
    Header("Tracking (50,000 narrow rows: shipments)");
    foreach (var mode in new[] { "tracking", "AsNoTracking" })
    {
        var tracked = 0;
        long kb = 0;
        await Measure(mode, async () =>
        {
            await using var db = Seed.Create(counter);
            var before = GC.GetAllocatedBytesForCurrentThread();
            var q = db.Shipments.OrderBy(s => s.Id).Take(50_000);
            var list = mode == "tracking" ? await q.ToListAsync() : await q.AsNoTracking().ToListAsync();
            kb = (GC.GetAllocatedBytesForCurrentThread() - before) / 1024;
            tracked = db.ChangeTracker.Entries().Count();
            return list.Count;
        }, runs: 7);
        Console.WriteLine($"{"",34} tracked entities={tracked,6}  allocated={kb,7} KB");
    }
    Header("Identity resolution (500 orders, 200 customers)");
    foreach (var mode in new[] { "tracking", "AsNoTracking", "AsNoTrackingWithIdentityResolution" })
    {
        await using var db = Seed.Create();
        var q = db.Orders.Select(o => new { o.Id, o.Customer }).OrderBy(x => x.Id).Take(500);
        var q2 = db.Orders.Include(o => o.Customer).OrderBy(o => o.Id).Take(500);
        q2 = mode switch { "AsNoTracking" => q2.AsNoTracking(), "AsNoTrackingWithIdentityResolution" => q2.AsNoTrackingWithIdentityResolution(), _ => q2 };
        var list = await q2.ToListAsync();
        Console.WriteLine($"{mode,-36} orders={list.Count} distinct Customer instances={list.Select(o => o.Customer).Distinct().Count()}");
    }
}

if (pill is "projection" or "all")
{
    Header("Whole entity vs projection (2,000 orders)");
    await Measure("whole entity", async () =>
    {
        await using var db = Seed.Create(counter);
        return (await db.Orders.AsNoTracking().OrderBy(o => o.Id).Take(2000).ToListAsync()).Count;
    });
    Console.WriteLine(counter.Sql[0]);
    await Measure("Select to record", async () =>
    {
        await using var db = Seed.Create(counter);
        return (await db.Orders.AsNoTracking().OrderBy(o => o.Id).Take(2000)
            .Select(o => new OrderRow(o.Id, o.Status, o.Total)).ToListAsync()).Count;
    });
    Console.WriteLine(counter.Sql[0]);
}

if (pill is "paging" or "all")
{
    Header("Pagination over 1,000,000 shipments");
    foreach (var page in new[] { 1, 1000, 20000, 40000 })
    {
        await Measure($"offset, page {page} (size 25)", async () =>
        {
            await using var db = Seed.Create(counter);
            return (await db.Shipments.AsNoTracking().OrderBy(s => s.ShippedAt).ThenBy(s => s.Id)
                .Skip((page - 1) * 25).Take(25).ToListAsync()).Count;
        }, runs: 15);
    }
    foreach (var page in new[] { 1, 1000, 20000, 40000 })
    {
        await using var db0 = Seed.Create();
        // The "cursor" the client would hold: last row of the previous page.
        var last = page == 1 ? null : await db0.Shipments.AsNoTracking().OrderBy(s => s.ShippedAt).ThenBy(s => s.Id)
            .Skip((page - 1) * 25 - 1).Take(1).FirstAsync();
        await Measure($"keyset, page {page} (size 25)", async () =>
        {
            await using var db = Seed.Create(counter);
            var q = db.Shipments.AsNoTracking().OrderBy(s => s.ShippedAt).ThenBy(s => s.Id).AsQueryable();
            if (last is not null)
                q = q.Where(s => s.ShippedAt > last.ShippedAt || (s.ShippedAt == last.ShippedAt && s.Id > last.Id));
            return (await q.Take(25).ToListAsync()).Count;
        }, runs: 15);
    }
}

if (pill is "in-memory" or "all")
{
    Header("Filtering in memory (active customers' pending orders)");
    await Measure("ToList().Where(...)", async () =>
    {
        await using var db = Seed.Create(counter);
        var all = await db.Orders.AsNoTracking().ToListAsync();
        return all.Where(o => o.Status == "Pending" && o.CustomerId == 7).ToList().Count;
    }, runs: 3);
    await Measure("Where(...).ToList()", async () =>
    {
        await using var db = Seed.Create(counter);
        return (await db.Orders.AsNoTracking().Where(o => o.Status == "Pending" && o.CustomerId == 7).ToListAsync()).Count;
    }, runs: 3);
}

if (pill is "cartesian" or "all")
{
    Header("Cartesian explosion (50 orders: 10 lines x 10 payments each)");
    foreach (var split in new[] { false, true })
    {
        var lines = 0; var pays = 0;
        await Measure(split ? "AsSplitQuery" : "AsSingleQuery", async () =>
        {
            await using var db = Seed.Create(counter);
            var q = db.Orders.OrderBy(o => o.Id).Take(50).Include(o => o.Lines).Include(o => o.Payments).AsNoTracking();
            var list = await (split ? q.AsSplitQuery() : q.AsSingleQuery()).ToListAsync();
            lines = list.Sum(o => o.Lines.Count); pays = list.Sum(o => o.Payments.Count);
            return list.Count;
        }, runs: 5);
        Console.WriteLine($"{"",34} loaded lines={lines} payments={pays}");
    }
    Console.WriteLine($"expected rows single query: 50 x 10 x 10 = {50 * 10 * 10}");
}

if (pill is "bulk" or "all")
{
    Header("SaveChanges in a loop vs ExecuteUpdate (expire 2,000 pending orders)");
    var ids = new List<int>();
    {
        await using var db = Seed.Create();
        ids = await db.Orders.Where(o => o.Status == "Pending").OrderBy(o => o.Id).Take(2000).Select(o => o.Id).ToListAsync();
    }
    // 1) SaveChanges per entity (entities loaded once, then one SaveChanges inside the loop)
    await Measure("SaveChanges per entity", async () =>
    {
        await using var db = Seed.Create(counter);
        var orders = await db.Orders.Where(o => ids.Contains(o.Id)).ToListAsync();
        foreach (var o in orders)
        {
            o.Status = "Expired";
            await db.SaveChangesAsync();
        }
        return 0;
    });
    await ResetStatus(ids);
    // 2) one SaveChanges
    await Measure("one SaveChanges", async () =>
    {
        await using var db = Seed.Create(counter);
        var orders = await db.Orders.Where(o => ids.Contains(o.Id)).ToListAsync();
        foreach (var o in orders) o.Status = "Expired";
        await db.SaveChangesAsync();
        return 0;
    });
    await ResetStatus(ids);
    // 3) ExecuteUpdate
    await Measure("ExecuteUpdate", async () =>
    {
        await using var db = Seed.Create(counter);
        return await db.Orders.Where(o => ids.Contains(o.Id)).ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, "Expired"));
    });
    await ResetStatus(ids);
}

if (pill is "any" or "all")
{
    Header("Count() > 0 vs Any()");
    foreach (var (label, filter) in new (string, System.Linq.Expressions.Expression<Func<Order, bool>>)[]
    {
        ("status = Pending (no index on Status alone)", o => o.Status == "Pending"),
        ("customer 7 + Pending (index CustomerId,Status)", o => o.CustomerId == 7 && o.Status == "Pending"),
        ("Total > 5000 (nobody matches)", o => o.Total > 5000),
    })
    {
        Console.WriteLine($"-- {label}");
        await Measure("Count() > 0", async () =>
        {
            await using var db = Seed.Create(counter);
            return await db.Orders.CountAsync(filter) > 0;
        }, runs: 15);
        await Measure("AnyAsync()", async () =>
        {
            await using var db = Seed.Create(counter);
            return await db.Orders.AnyAsync(filter);
        }, runs: 15);
    }
    Console.WriteLine("-- 1,000,000 shipments, Carrier = 'Carrier 3' (no index, thousands of matches)");
    await Measure("Count() > 0", async () =>
    {
        await using var db = Seed.Create(counter);
        return await db.Shipments.CountAsync(s => s.Carrier == "Carrier 3") > 0;
    }, runs: 7);
    await Measure("AnyAsync()", async () =>
    {
        await using var db = Seed.Create(counter);
        return await db.Shipments.AnyAsync(s => s.Carrier == "Carrier 3");
    }, runs: 7);
}

async Task ResetStatus(List<int> ids)
{
    await using var db = Seed.Create();
    await db.Orders.Where(o => ids.Contains(o.Id)).ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, "Pending"));
}

record OrderRow(int Id, string Status, decimal Total);
