using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EfCoreInterceptors.Interceptors.Transactions;

/// <summary>Logs how each transaction ends. SaveChanges opens one implicitly when it writes.</summary>
public class TransactionLogInterceptor : DbTransactionInterceptor
{
    public override void TransactionCommitted(DbTransaction transaction, TransactionEndEventData eventData)
    {
        Console.WriteLine($"  [transaction] committed {eventData.TransactionId}");
    }

    public override Task TransactionCommittedAsync(
        DbTransaction transaction,
        TransactionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        TransactionCommitted(transaction, eventData);
        return Task.CompletedTask;
    }

    public override void TransactionRolledBack(DbTransaction transaction, TransactionEndEventData eventData)
    {
        Console.WriteLine($"  [transaction] rolled back {eventData.TransactionId}");
    }

    public override Task TransactionRolledBackAsync(
        DbTransaction transaction,
        TransactionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        TransactionRolledBack(transaction, eventData);
        return Task.CompletedTask;
    }
}
