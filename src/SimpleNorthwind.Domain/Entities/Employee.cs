namespace SimpleNorthwind.Domain.Entities;

public sealed class Employee
{
    public int EmployeeId { get; set; }
    public string Password { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? Title { get; set; }
    public DateTime? BirthDate { get; set; }
    public DateTime? HireDate { get; set; }
    public string? PhoneExtNumber { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Notes { get; set; }
    public bool IsResigned { get; set; }
    public DateTime? ResignDate { get; set; }
}
