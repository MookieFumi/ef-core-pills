using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EfCorePills.Diagnostics;

/// <summary>Counts the commands sent to the database and the rows read, and keeps the SQL text.</summary>
public class CommandCounter : DbCommandInterceptor
{
    public int Count { get; private set; }

    public long Rows { get; private set; }

    public List<string> Sql { get; } = [];

    public void Reset()
    {
        Count = 0;
        Rows = 0;
        Sql.Clear();
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        Register(command);
        return new ValueTask<InterceptionResult<DbDataReader>>(result);
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        Register(command);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Register(command);
        return new ValueTask<InterceptionResult<int>>(result);
    }

    public override InterceptionResult DataReaderClosing(
        DbCommand command,
        DataReaderClosingEventData eventData,
        InterceptionResult result)
    {
        CountRows(eventData);
        return result;
    }

    public override ValueTask<InterceptionResult> DataReaderClosingAsync(
        DbCommand command,
        DataReaderClosingEventData eventData,
        InterceptionResult result)
    {
        CountRows(eventData);
        return new ValueTask<InterceptionResult>(result);
    }

    private void Register(DbCommand command)
    {
        Count++;
        Sql.Add(command.CommandText);
    }

    // ReadCount includes the final Read() that returns false.
    private void CountRows(DataReaderClosingEventData eventData)
    {
        Rows += Math.Max(0, eventData.ReadCount - 1);
    }
}
