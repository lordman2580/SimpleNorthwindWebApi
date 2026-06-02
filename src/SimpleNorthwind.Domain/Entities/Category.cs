namespace SimpleNorthwind.Domain.Entities;

public sealed class Category
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
}
