using EfCoreInterceptors.Data;
using EfCoreInterceptors.Entities;
using EfCoreInterceptors.Interceptors.Queries;
using Microsoft.EntityFrameworkCore;

namespace EfCoreInterceptors.Demos;

public class QueryExpressionDemo : IDemo
{
    public string Name => "query";

    public string Title => "6. IQueryExpressionInterceptor: change the LINQ before it becomes SQL";

    public async Task RunAsync()
    {
        await using var db = ShopContextFactory.CreateRecreated("query.db", new RowCapQueryInterceptor(maxRows: 5));
        db.Products.AddRange(Enumerable.Range(1, 20).Select(i => new Product { Name = $"Product {i}", Price = i }));
        await db.SaveChangesAsync();

        var all = await db.Products.ToListAsync();
        Console.WriteLine($"  ToList() without Take: {all.Count} rows (table has 20)");

        var three = await db.Products.OrderBy(p => p.Id).Take(3).ToListAsync();
        Console.WriteLine($"  Take(3): {three.Count} rows");

        Console.WriteLine($"  Count(): {await db.Products.CountAsync()}");
    }
}
