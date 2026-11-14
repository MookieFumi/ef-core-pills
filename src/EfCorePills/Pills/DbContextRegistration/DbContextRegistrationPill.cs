using EfCorePills.Data;
using EfCorePills.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EfCorePills.Pills.DbContextRegistration;

public class DbContextRegistrationPill : IPill
{
    private const int Requests = 5_000;

    public string Name => "registration";

    public async Task RunAsync(Measurer measurer)
    {
        measurer.Header("AddDbContext, AddDbContextPool and AddDbContextFactory");

        await ShowLifetimeAsync(measurer);
        await ShowPoolReuseAsync(measurer);
        await MeasureRequestsAsync(measurer);
        await ShowParallelWorkAsync(measurer);
        ShowFactoryRegistration(measurer);
    }

    private static ServiceProvider BuildScoped(CommandCounter counter)
    {
        var services = new ServiceCollection();
        services.AddDbContext<ShopContext>(options => Configure(options, counter));
        return services.BuildServiceProvider();
    }

    private static ServiceProvider BuildPooled(CommandCounter counter)
    {
        var services = new ServiceCollection();
        services.AddDbContextPool<ShopContext>(options => Configure(options, counter), poolSize: 128);
        return services.BuildServiceProvider();
    }

    private static ServiceProvider BuildFactory(CommandCounter counter)
    {
        var services = new ServiceCollection();
        services.AddDbContextFactory<ShopContext>(options => Configure(options, counter));
        return services.BuildServiceProvider();
    }

    private static void Configure(DbContextOptionsBuilder options, CommandCounter counter)
    {
        options.UseSqlite($"Data Source={ShopContextFactory.DatabasePath}").AddInterceptors(counter);
    }

    private static async Task ShowLifetimeAsync(Measurer measurer)
    {
        measurer.Note("-- AddDbContext: one instance per scope (one scope = one HTTP request)");

        await using var provider = BuildScoped(measurer.Counter);

        await using var first = provider.CreateAsyncScope();
        var a = first.ServiceProvider.GetRequiredService<ShopContext>();
        var b = first.ServiceProvider.GetRequiredService<ShopContext>();

        await using var second = provider.CreateAsyncScope();
        var c = second.ServiceProvider.GetRequiredService<ShopContext>();

        measurer.Note($"same scope, resolved twice -> same instance: {ReferenceEquals(a, b)}");
        measurer.Note($"different scopes            -> same instance: {ReferenceEquals(a, c)}");
    }

    private static async Task ShowPoolReuseAsync(Measurer measurer)
    {
        measurer.Header("AddDbContextPool: instances are reused and reset");

        await using var provider = BuildPooled(measurer.Counter);
        var distinct = new HashSet<ShopContext>(ReferenceEqualityComparer.Instance);
        var trackedAtStart = 0;

        for (var i = 0; i < Requests; i++)
        {
            await using var scope = provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ShopContext>();
            distinct.Add(db);
            trackedAtStart += db.ChangeTracker.Entries().Count();

            // Leave something tracked: the pool must clean it before the next request.
            await db.Customers.OrderBy(c => c.Id).FirstAsync();
        }

        measurer.Note($"sequential scopes: {Requests}  distinct DbContext instances: {distinct.Count}");
        measurer.Note($"entities tracked when a request started (should be 0): {trackedAtStart}");
    }

    private static async Task MeasureRequestsAsync(Measurer measurer)
    {
        measurer.Header($"{Requests:N0} sequential 'requests' (new scope + one tiny query)");

        await using var scoped = BuildScoped(measurer.Counter);
        await using var pooled = BuildPooled(measurer.Counter);
        await using var factory = BuildFactory(measurer.Counter);

        await measurer.MeasureAsync("AddDbContext", () => RunRequestsAsync(scoped), runs: 7);
        await measurer.MeasureAsync("AddDbContextPool", () => RunRequestsAsync(pooled), runs: 7);
        await measurer.MeasureAsync(
            "AddDbContextFactory",
            async () =>
            {
                var factoryInstance = factory.GetRequiredService<IDbContextFactory<ShopContext>>();

                for (var i = 0; i < Requests; i++)
                {
                    await using var db = await factoryInstance.CreateDbContextAsync();
                    await db.Customers.AsNoTracking().OrderBy(c => c.Id).FirstAsync();
                }

                return Requests;
            },
            runs: 7);
    }

    private static async Task<int> RunRequestsAsync(ServiceProvider provider)
    {
        for (var i = 0; i < Requests; i++)
        {
            await using var scope = provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ShopContext>();
            await db.Customers.AsNoTracking().OrderBy(c => c.Id).FirstAsync();
        }

        return Requests;
    }

    private static async Task ShowParallelWorkAsync(Measurer measurer)
    {
        measurer.Header("Parallel work: one shared context vs one context per task");

        await using var scoped = BuildScoped(measurer.Counter);
        await using var scope = scoped.CreateAsyncScope();
        var shared = scope.ServiceProvider.GetRequiredService<ShopContext>();

        try
        {
            var shareds = Enumerable.Range(0, 8)
                .Select(_ => Task.Run(() => shared.Customers.CountAsync()))
                .ToArray();
            await Task.WhenAll(shareds);
            measurer.Note("shared scoped context, 8 parallel queries: no exception this time (not guaranteed)");
        }
        catch (Exception ex)
        {
            measurer.Note($"shared scoped context, 8 parallel queries: {ex.GetType().Name}");
        }

        await using var factoryProvider = BuildFactory(measurer.Counter);
        var factory = factoryProvider.GetRequiredService<IDbContextFactory<ShopContext>>();

        var counts = await Task.WhenAll(Enumerable.Range(0, 8).Select(async _ =>
        {
            await using var db = await factory.CreateDbContextAsync();
            return await db.Customers.CountAsync();
        }));

        measurer.Note($"factory, 8 parallel queries (one context each): {counts.Length} results, all {counts[0]}");
    }

    private static void ShowFactoryRegistration(Measurer measurer)
    {
        measurer.Header("What AddDbContextFactory registers");

        using var provider = BuildFactory(measurer.Counter);
        var factory = provider.GetRequiredService<IDbContextFactory<ShopContext>>();
        var scopedContext = provider.GetService<ShopContext>();

        measurer.Note($"IDbContextFactory<ShopContext> registered: {factory is not null}");
        measurer.Note($"ShopContext resolvable straight from the container: {scopedContext is not null}");
    }
}
