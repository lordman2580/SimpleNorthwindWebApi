using SimpleNorthwind.Domain.Entities;

namespace SimpleNorthwind.Application.Abstractions.Persistence;

public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(int employeeId, CancellationToken ct = default);
}
