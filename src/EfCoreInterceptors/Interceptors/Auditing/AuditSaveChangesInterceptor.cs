using EfCoreInterceptors.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EfCoreInterceptors.Interceptors.Auditing;

/// <summary>Fills CreatedOn/UpdatedOn and turns deletes into soft deletes, right before SaveChanges runs.</summary>
public class AuditSaveChangesInterceptor(TimeProvider clock) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return new ValueTask<InterceptionResult<int>>(result);
    }

    private void Apply(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = clock.GetUtcNow().UtcDateTime;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry is { State: EntityState.Added, Entity: IAuditable added })
            {
                added.CreatedOn = now;
            }
            else if (entry is { State: EntityState.Modified, Entity: IAuditable modified })
            {
                modified.UpdatedOn = now;
            }
            else if (entry is { State: EntityState.Deleted, Entity: ISoftDeletable deleted })
            {
                deleted.IsDeleted = true;
                entry.State = EntityState.Modified;

                if (entry.Entity is IAuditable auditable)
                {
                    auditable.UpdatedOn = now;
                }
            }
        }
    }
}
