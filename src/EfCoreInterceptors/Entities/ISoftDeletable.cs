namespace EfCoreInterceptors.Entities;

public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
}
