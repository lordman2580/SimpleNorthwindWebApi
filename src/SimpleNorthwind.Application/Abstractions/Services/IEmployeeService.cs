using SimpleNorthwind.Application.Employees;

namespace SimpleNorthwind.Application.Abstractions.Services;

public interface IEmployeeService
{
    /// <summary>列出全部員工（唯讀，投影時丟棄 password）。</summary>
    Task<IReadOnlyList<EmployeeDto>> ListAsync(CancellationToken ct = default);
}
