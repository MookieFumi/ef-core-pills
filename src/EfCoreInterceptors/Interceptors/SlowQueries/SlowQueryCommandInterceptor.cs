using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EfCoreInterceptors.Interceptors.SlowQueries;

/// <summary>Reports every command that takes longer than the threshold, with the SQL EF Core really sent.</summary>
public class SlowQueryCommandInterceptor(TimeSpan threshold) : DbCommandInterceptor
{
    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        Report(command, eventData);
        return result;
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        Report(command, eventData);
        return new ValueTask<DbDataReader>(result);
    }

    private void Report(DbCommand command, CommandExecutedEventData eventData)
    {
        if (eventData.Duration >= threshold)
        {
            Console.WriteLine($"  [slow query] {eventData.Duration.TotalMilliseconds:F0} ms >= {threshold.TotalMilliseconds:F0} ms");
            Console.WriteLine($"  {command.CommandText.ReplaceLineEndings(" ")}");
        }
    }
}
