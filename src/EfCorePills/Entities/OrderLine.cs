namespace EfCorePills.Entities;

public class OrderLine
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public string Sku { get; set; } = "";

    public int Quantity { get; set; }
}
