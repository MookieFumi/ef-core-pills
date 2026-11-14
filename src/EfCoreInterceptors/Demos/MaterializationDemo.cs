using EfCoreInterceptors.Data;
using EfCoreInterceptors.Entities;
using EfCoreInterceptors.Interceptors.Materialization;
using Microsoft.EntityFrameworkCore;

namespace EfCoreInterceptors.Demos;

public class MaterializationDemo : IDemo
{
    public string Name => "materialization";

    public string Title => "5. IMaterializationInterceptor: set a property that is not in the table";

    public async Task RunAsync()
    {
        await using var db = ShopContextFactory.CreateRecreated("materialization.db", new LoadedAtMaterializationInterceptor(TimeProvider.System));
        db.Products.Add(new Product { Name = "Headset", Price = 80 });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var product = await db.Products.SingleAsync();
        Console.WriteLine($"  LoadedAt (not mapped) = {product.LoadedAt:O}");
    }
}
