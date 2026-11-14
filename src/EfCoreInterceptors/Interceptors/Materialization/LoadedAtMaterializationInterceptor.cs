using EfCoreInterceptors.Entities;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EfCoreInterceptors.Interceptors.Materialization;

/// <summary>Fills a property that is not mapped to any column, every time EF Core builds a Product.</summary>
public class LoadedAtMaterializationInterceptor(TimeProvider clock) : IMaterializationInterceptor
{
    public object InitializedInstance(MaterializationInterceptionData materializationData, object instance)
    {
        if (instance is Product product)
        {
            product.LoadedAt = clock.GetUtcNow().UtcDateTime;
        }

        return instance;
    }
}
