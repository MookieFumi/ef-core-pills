using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EfCorePills;

/// <summary>Counts commands sent to the database and keeps the SQL text.</summary>
public class CommandCounter : DbCommandInterceptor
{
    public int Count { get; private set; }
    public long Rows { get; private set; }
    public List<string> Sql { get; } = [];

    public void Reset() { Count = 0; Rows = 0; Sql.Clear(); }

    public override InterceptionResult DataReaderClosing(
        DbCommand command, DataReaderClosingEventData eventData, InterceptionResult result)
    { Rows += Math.Max(0, eventData.ReadCount - 1); return result; }

    public override ValueTask<InterceptionResult> DataReaderClosingAsync(
        DbCommand command, DataReaderClosingEventData eventData, InterceptionResult result)
    { Rows += Math.Max(0, eventData.ReadCount - 1); return new(result); }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
        CancellationToken ct = default)
    { Count++; Sql.Add(command.CommandText); return new(result); }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    { Count++; Sql.Add(command.CommandText); return result; }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<int> result,
        CancellationToken ct = default)
    { Count++; Sql.Add(command.CommandText); return new(result); }
}
