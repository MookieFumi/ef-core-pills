using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EfCoreInterceptors.Data;

public static class ShopContextFactory
{
    public static ShopContext Create(string databaseFile, params IInterceptor[] interceptors)
    {
        var options = new DbContextOptionsBuilder<ShopContext>()
            .UseSqlite($"Data Source={databaseFile}")
            .AddInterceptors(interceptors);

        return new ShopContext(options.Options);
    }

    public static ShopContext CreateRecreated(string databaseFile, params IInterceptor[] interceptors)
    {
        using (var setup = Create(databaseFile))
        {
            setup.Database.EnsureDeleted();
            setup.Database.EnsureCreated();
        }

        return Create(databaseFile, interceptors);
    }
}
