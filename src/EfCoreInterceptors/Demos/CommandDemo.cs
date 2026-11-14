using EfCoreInterceptors.Data;
using EfCoreInterceptors.Entities;
using EfCoreInterceptors.Interceptors.SlowQueries;
using Microsoft.EntityFrameworkCore;

namespace EfCoreInterceptors.Demos;

public class CommandDemo : IDemo
{
    public string Name => "command";

    public string Title => "2. IDbCommandInterceptor: see the SQL that really goes out";

    public async Task RunAsync()
    {
        // Threshold of zero so that every command is reported in the demo. In real code use something like 500 ms.
        await using var db = ShopContextFactory.CreateRecreated("command.db", new SlowQueryCommandInterceptor(TimeSpan.Zero));
        db.Products.Add(new Product { Name = "Mouse", Price = 20 });
        await db.SaveChangesAsync();

        var cheap = await db.Products.Where(p => p.Price < 100).ToListAsync();
        Console.WriteLine($"  products read: {cheap.Count}");
    }
}
