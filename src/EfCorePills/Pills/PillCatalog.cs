using EfCorePills.Pills.AnyVsCount;
using EfCorePills.Pills.BulkUpdate;
using EfCorePills.Pills.CartesianExplosion;
using EfCorePills.Pills.InMemoryFilter;
using EfCorePills.Pills.NPlusOne;
using EfCorePills.Pills.Pagination;
using EfCorePills.Pills.Projection;
using EfCorePills.Pills.Tracking;

namespace EfCorePills.Pills;

public static class PillCatalog
{
    public static IReadOnlyList<IPill> All { get; } =
    [
        new NPlusOnePill(),
        new TrackingPill(),
        new ProjectionPill(),
        new PaginationPill(),
        new InMemoryFilterPill(),
        new CartesianExplosionPill(),
        new BulkUpdatePill(),
        new AnyVsCountPill(),
    ];
}
