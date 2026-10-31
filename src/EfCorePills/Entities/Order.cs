namespace EfCorePills.Entities;

public class Order
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    public Customer Customer { get; set; } = null!;

    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; }

    public decimal Total { get; set; }

    // Simulates the wide columns of a real table: notes, an attached PDF, etc.
    public string Notes { get; set; } = "";

    public byte[] InvoicePdf { get; set; } = [];

    public List<OrderLine> Lines { get; set; } = [];

    public List<Payment> Payments { get; set; } = [];
}
