namespace SimpleNorthwind.Domain.Entities;

public sealed class Order
{
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public int EmployeeId { get; set; }
    public DateTime OrderDate { get; set; }
    public int? ModifiedEmployeeId { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public bool IsCanceled { get; set; }
    public bool IsPaidoff { get; set; }
}
