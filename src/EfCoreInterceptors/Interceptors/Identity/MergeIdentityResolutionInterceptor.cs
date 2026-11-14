using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EfCoreInterceptors.Interceptors.Identity;

/// <summary>When a second instance with an already tracked key arrives, copy its values instead of throwing.</summary>
public class MergeIdentityResolutionInterceptor : IIdentityResolutionInterceptor
{
    public void UpdateTrackedInstance(IdentityResolutionInterceptionData interceptionData, EntityEntry existingEntry, object newInstance)
    {
        Console.WriteLine($"  [identity] key already tracked, merging values into the existing {existingEntry.Metadata.ShortName()}");
        existingEntry.CurrentValues.SetValues(newInstance);
    }
}
