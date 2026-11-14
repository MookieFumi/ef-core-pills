using EfCoreInterceptors.Data;
using EfCoreInterceptors.Entities;
using EfCoreInterceptors.Interceptors.Identity;
using Microsoft.EntityFrameworkCore;

namespace EfCoreInterceptors.Demos;

public class IdentityResolutionDemo : IDemo
{
    public string Name => "identity";

    public string Title => "7. IIdentityResolutionInterceptor: two instances, one key";

    public async Task RunAsync()
    {
        await using var seed = ShopContextFactory.CreateRecreated("identity.db");
        seed.Products.Add(new Product { Name = "Dock", Price = 100 });
        await seed.SaveChangesAsync();

        Console.WriteLine("  without interceptor:");
        await using (var plain = ShopContextFactory.Create("identity.db"))
        {
            var tracked = await plain.Products.SingleAsync();
            try
            {
                plain.Attach(new Product { Id = tracked.Id, Name = "Dock v2", Price = 120 });
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"  {ex.GetType().Name}: {ex.Message.Split('.')[0]}.");
            }
        }

        Console.WriteLine("  with interceptor:");
        await using var merged = ShopContextFactory.Create("identity.db", new MergeIdentityResolutionInterceptor());
        var existing = await merged.Products.SingleAsync();
        merged.Attach(new Product { Id = existing.Id, Name = "Dock v2", Price = 120 });
        Console.WriteLine($"  tracked product is now '{existing.Name}' at {existing.Price}");
    }
}
