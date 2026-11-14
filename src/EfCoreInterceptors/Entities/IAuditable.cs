namespace EfCoreInterceptors.Entities;

public interface IAuditable
{
    DateTime CreatedOn { get; set; }

    DateTime? UpdatedOn { get; set; }
}
