namespace SimpleNorthwind.Domain.Entities;

public sealed class OrderDetail
{
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int OrderQuantities { get; set; }
    public decimal Discount { get; set; }
    public int Version { get; set; }
}
