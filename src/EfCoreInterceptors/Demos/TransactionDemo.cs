using EfCoreInterceptors.Data;
using EfCoreInterceptors.Entities;
using EfCoreInterceptors.Interceptors.Transactions;
using Microsoft.EntityFrameworkCore;

namespace EfCoreInterceptors.Demos;

public class TransactionDemo : IDemo
{
    public string Name => "transaction";

    public string Title => "4. IDbTransactionInterceptor: commit and rollback";

    public async Task RunAsync()
    {
        await using var db = ShopContextFactory.CreateRecreated("transaction.db", new TransactionLogInterceptor());

        // Two statements, so SaveChanges wraps them in a transaction. A single statement runs without one.
        db.Products.Add(new Product { Name = "Monitor", Price = 200 });
        db.Products.Add(new Product { Name = "Webcam", Price = 50 });
        await db.SaveChangesAsync();

        // Name has a unique index, so the second row fails and the whole SaveChanges is rolled back.
        // EF Core disposes the transaction without calling Rollback, so the interceptor sees nothing here.
        db.Products.Add(new Product { Name = "Keyboard", Price = 40 });
        db.Products.Add(new Product { Name = "Monitor", Price = 210 });

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            Console.WriteLine("  SaveChanges failed (unique index)");
        }

        // An explicit transaction goes through the same interceptor.
        await using var transaction = await db.Database.BeginTransactionAsync();
        await transaction.RollbackAsync();
    }
}
