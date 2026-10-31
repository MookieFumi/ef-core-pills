using EfCorePills.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace EfCorePills.Data;

public static class ShopContextFactory
{
    public const string DatabasePath = "pills.db";

    public static ShopContext Create(CommandCounter? counter = null)
    {
        var options = new DbContextOptionsBuilder<ShopContext>().UseSqlite($"Data Source={DatabasePath}");

        if (counter is not null)
        {
            options.AddInterceptors(counter);
        }

        return new ShopContext(options.Options);
    }
}
