namespace EfCoreInterceptors.Demos;

public static class DemoCatalog
{
    public static IReadOnlyList<IDemo> All { get; } =
    [
        new SaveChangesDemo(),
        new CommandDemo(),
        new ConnectionDemo(),
        new TransactionDemo(),
        new MaterializationDemo(),
        new QueryExpressionDemo(),
        new IdentityResolutionDemo(),
    ];
}
