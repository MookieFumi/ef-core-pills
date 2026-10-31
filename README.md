# ef-core-pills

Runnable demos for the EF Core pills series on [mookiefumi.github.io](https://mookiefumi.github.io).

One console app, a SQLite database seeded on first run (20,000 orders with 10 lines and 10 payments each,
200 customers and 1,000,000 shipments) and one folder per pill. Every pill prints the number of commands
sent to the database, the rows read and the median time.

```bash
cd src/EfCorePills
dotnet run -c Release                 # all pills
dotnet run -c Release -- n-plus-one   # tracking | projection | paging | in-memory | cartesian | bulk | any
```

Requires the .NET 10 SDK. The first run creates `pills.db` (about 500 MB, mostly the simulated PDF column) and takes a minute.
Command and row counts are deterministic; times depend on the machine, so compare orders of magnitude, not decimals.
`results.txt` holds the output used in the posts.

## Layout

```
src/EfCorePills/
  Program.cs              runs the pills selected on the command line
  Entities/               one class per table
  Data/                   ShopContext, the context factory and the database seeder
  Diagnostics/            CommandCounter (EF interceptor) and Measurer (timing and output)
  Pills/                  one folder per pill, each with its IPill implementation
    NPlusOne/             n-plus-one
    Tracking/             tracking
    Projection/           projection
    Pagination/           paging
    InMemoryFilter/       in-memory
    CartesianExplosion/   cartesian
    BulkUpdate/           bulk
    AnyVsCount/           any
```

Code style: one type per file, file-scoped namespaces and braces on every block (see `.editorconfig`).
