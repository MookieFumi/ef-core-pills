using EfCoreInterceptors.Data;
using EfCoreInterceptors.Entities;
using EfCoreInterceptors.Interceptors.Auditing;
using Microsoft.EntityFrameworkCore;

namespace EfCoreInterceptors.Demos;

public class SaveChangesDemo : IDemo
{
    public string Name => "save";

    public string Title => "1. ISaveChangesInterceptor: audit fields and soft delete";

    public async Task RunAsync()
    {
        await using var db = ShopContextFactory.CreateRecreated("save.db", new AuditSaveChangesInterceptor(TimeProvider.System));

        var keyboard = new Product { Name = "Keyboard", Price = 40 };
        db.Products.Add(keyboard);
        await db.SaveChangesAsync();
        Console.WriteLine($"  after insert: CreatedOn={keyboard.CreatedOn:O} UpdatedOn={keyboard.UpdatedOn?.ToString("O") ?? "null"}");

        keyboard.Price = 45;
        await db.SaveChangesAsync();
        Console.WriteLine($"  after update: UpdatedOn={keyboard.UpdatedOn:O}");

        db.Products.Remove(keyboard);
        await db.SaveChangesAsync();

        var visible = await db.Products.CountAsync();
        var inTable = await db.Products.IgnoreQueryFilters().CountAsync();
        Console.WriteLine($"  after Remove: visible={visible}, rows in table={inTable}, IsDeleted={keyboard.IsDeleted}");
    }
}
