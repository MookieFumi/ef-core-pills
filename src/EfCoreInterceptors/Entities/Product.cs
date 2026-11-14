using System.ComponentModel.DataAnnotations.Schema;

namespace EfCoreInterceptors.Entities;

public class Product : IAuditable, ISoftDeletable
{
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public decimal Price { get; set; }

    public DateTime CreatedOn { get; set; }

    public DateTime? UpdatedOn { get; set; }

    public bool IsDeleted { get; set; }

    // Not mapped: filled by the materialization interceptor every time EF Core builds the entity.
    [NotMapped]
    public DateTime? LoadedAt { get; set; }
}
