namespace SimpleNorthwind.Application.Employees;

/// <summary>員工唯讀檢視 DTO。⚠️ 絕不含 password / 雜湊；birth_date / notes 等 PII 預設不輸出。FullName = first + last。</summary>
public sealed record EmployeeDto(
    int EmployeeId,
    string FirstName,
    string LastName,
    string FullName,
    string? Title,
    string? PhoneNumber,
    string? PhoneExtNumber,
    bool IsResigned,
    DateTime? HireDate);
