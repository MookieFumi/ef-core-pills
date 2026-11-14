using EfCoreInterceptors.Demos;

var requested = args.FirstOrDefault() ?? "all";

foreach (var demo in DemoCatalog.All)
{
    if (requested is "all" || requested == demo.Name)
    {
        Console.WriteLine();
        Console.WriteLine(demo.Title);
        await demo.RunAsync();
    }
}
