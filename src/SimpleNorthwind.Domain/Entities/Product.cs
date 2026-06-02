namespace SimpleNorthwind.Domain.Entities;

public sealed class Product
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public int Quantities { get; set; }
    public decimal UnitPrice { get; set; }
}
