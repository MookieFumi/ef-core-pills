using System.Diagnostics;

namespace EfCorePills.Diagnostics;

/// <summary>Runs a piece of code, counts what it sends to the database and prints one line per measurement.</summary>
public class Measurer(CommandCounter counter)
{
    public CommandCounter Counter => counter;

    public void Header(string title)
    {
        Console.WriteLine();
        Console.WriteLine($"=== {title} ===");
    }

    public void Note(string text)
    {
        Console.WriteLine(text);
    }

    /// <summary>With runs greater than one, does a warm-up run and reports the median of the measured runs.</summary>
    public async Task<T> MeasureAsync<T>(string label, Func<Task<T>> run, int runs = 1)
    {
        if (runs > 1)
        {
            await run();
        }

        var times = new List<long>();
        T result = default!;

        for (var i = 0; i < runs; i++)
        {
            counter.Reset();
            var stopwatch = Stopwatch.StartNew();
            result = await run();
            stopwatch.Stop();
            times.Add(stopwatch.ElapsedMilliseconds);
        }

        times.Sort();
        var median = times[times.Count / 2];
        Console.WriteLine($"{label,-34} commands={counter.Count,6}  rows={counter.Rows,8}  median={median,6} ms (n={runs})");

        return result;
    }
}
