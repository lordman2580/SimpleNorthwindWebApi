namespace SimpleNorthwind.Application.Customers;

public sealed record CustomerDto(
    int CustomerId,
    string CompanyName,
    string? ContactNumber,
    string? ContactTitle,
    DateTime CreateDate,
    string CreateUser,
    bool IsOutContacted,
    DateTime? OutContactedDate);

public sealed record CreateCustomerRequest(
    string CompanyName,
    string? ContactNumber,
    string? ContactTitle);

public sealed record UpdateCustomerRequest(
    string CompanyName,
    string? ContactNumber,
    string? ContactTitle,
    bool IsOutContacted,
    DateTime? OutContactedDate);
