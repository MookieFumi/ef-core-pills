namespace EfCorePills.Entities;

public class Shipment
{
    public int Id { get; set; }

    public string Carrier { get; set; } = "";

    public DateTime ShippedAt { get; set; }
}
