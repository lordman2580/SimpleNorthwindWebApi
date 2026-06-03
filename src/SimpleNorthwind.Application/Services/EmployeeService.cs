using SimpleNorthwind.Application.Abstractions.Persistence;
using SimpleNorthwind.Application.Abstractions.Services;
using SimpleNorthwind.Application.Employees;

namespace SimpleNorthwind.Application.Services;

/// <summary>員工唯讀查詢。投影為 <see cref="EmployeeDto"/> 時**即丟棄 password**（絕不外露雜湊）。</summary>
public sealed class EmployeeService(IEmployeeRepository employees) : IEmployeeService
{
    public async Task<IReadOnlyList<EmployeeDto>> ListAsync(CancellationToken ct = default)
    {
        var list = await employees.ListAsync(ct).ConfigureAwait(false);
        return list.Select(e => new EmployeeDto(
            e.EmployeeId, e.FirstName, e.LastName, $"{e.FirstName} {e.LastName}",
            e.Title, e.PhoneNumber, e.PhoneExtNumber, e.IsResigned, e.HireDate)).ToList();
    }
}
