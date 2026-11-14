using EfCoreInterceptors.Data;
using EfCoreInterceptors.Entities;
using EfCoreInterceptors.Interceptors.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace EfCoreInterceptors.Demos;

public class ConnectionDemo : IDemo
{
    public string Name => "connection";

    public string Title => "3. IDbConnectionInterceptor: one database per tenant";

    public async Task RunAsync()
    {
        foreach (var file in new[] { "tenant-a.db", "tenant-b.db" })
        {
            using var setup = ShopContextFactory.Create(file);
            setup.Database.EnsureDeleted();
            setup.Database.EnsureCreated();
        }

        var tenant = new TenantContext();
        await using var db = ShopContextFactory.Create("ignored.db", new TenantConnectionInterceptor(tenant));

        tenant.Current = "tenant-a";
        db.Products.Add(new Product { Name = "Only in A", Price = 1 });
        await db.SaveChangesAsync();

        // The same DbContext instance keeps its connection string, so a new one is needed per tenant.
        // In a web app the context is scoped to the request, so this is what already happens.
        tenant.Current = "tenant-b";
        await using var dbB = ShopContextFactory.Create("ignored.db", new TenantConnectionInterceptor(tenant));
        Console.WriteLine($"  products seen as tenant-b: {await dbB.Products.CountAsync()}");

        tenant.Current = "tenant-a";
        await using var dbA = ShopContextFactory.Create("ignored.db", new TenantConnectionInterceptor(tenant));
        Console.WriteLine($"  products seen as tenant-a: {await dbA.Products.CountAsync()}");
    }
}
