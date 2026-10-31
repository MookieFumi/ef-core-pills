using EfCorePills.Data;
using EfCorePills.Diagnostics;
using EfCorePills.Pills;

DatabaseSeeder.EnsureSeeded();

var requested = args.FirstOrDefault() ?? "all";
var measurer = new Measurer(new CommandCounter());

foreach (var pill in PillCatalog.All)
{
    if (requested is "all" || requested == pill.Name)
    {
        await pill.RunAsync(measurer);
    }
}
