namespace SimpleNorthwind.Domain.Entities;

public sealed class Customer
{
    public int CustomerId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? ContactNumber { get; set; }
    public string? ContactTitle { get; set; }
    public DateTime CreateDate { get; set; }
    public string CreateUser { get; set; } = string.Empty;
    public bool IsOutContacted { get; set; }
    public DateTime? OutContactedDate { get; set; }
}
