using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EfCoreInterceptors.Interceptors.Tenancy;

/// <summary>Points the connection at the current tenant's database just before it opens.</summary>
public class TenantConnectionInterceptor(TenantContext tenant) : DbConnectionInterceptor
{
    public override InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
    {
        SwitchDatabase(connection);
        return result;
    }

    public override ValueTask<InterceptionResult> ConnectionOpeningAsync(
        DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result,
        CancellationToken cancellationToken = default)
    {
        SwitchDatabase(connection);
        return new ValueTask<InterceptionResult>(result);
    }

    private void SwitchDatabase(DbConnection connection)
    {
        connection.ConnectionString = $"Data Source={tenant.Current}.db";
        Console.WriteLine($"  [connection] opening {tenant.Current}.db");
    }
}
