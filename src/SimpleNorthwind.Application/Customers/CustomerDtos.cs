namespace SimpleNorthwind.Application.Customers;

// ContactName / Email 為前端設計稿新增欄位（UD12，migration 0011）。
public sealed record CustomerDto(
    int CustomerId,
    string CompanyName,
    string? ContactName,
    string? ContactNumber,
    string? ContactTitle,
    string? Email,
    DateTime CreateDate,
    string CreateUser,
    bool IsOutContacted,
    DateTime? OutContactedDate);

public sealed record CreateCustomerRequest(
    string CompanyName,
    string? ContactName,
    string? ContactNumber,
    string? ContactTitle,
    string? Email);

public sealed record UpdateCustomerRequest(
    string CompanyName,
    string? ContactName,
    string? ContactNumber,
    string? ContactTitle,
    string? Email,
    bool IsOutContacted,
    DateTime? OutContactedDate);
