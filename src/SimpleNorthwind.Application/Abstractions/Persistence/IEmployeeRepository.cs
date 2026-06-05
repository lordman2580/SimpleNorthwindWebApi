using SimpleNorthwind.Domain.Entities;

namespace SimpleNorthwind.Application.Abstractions.Persistence;

public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(int employeeId, CancellationToken ct = default);

    /// <summary>依姓名（first + last）查員工（SQL Server 預設 CI collation 比對）。
    /// case-sensitive 由 <c>AuthService.LoginByNameAsync</c> 以序數比對把關；UQ_employees_name 保證至多一筆 CI 相符。</summary>
    Task<IReadOnlyList<Employee>> FindByNameAsync(string firstName, string lastName, CancellationToken ct = default);

    /// <summary>列出全部員工（唯讀檢視 / Dashboard 用）。投影為 DTO 時丟棄 password。</summary>
    Task<IReadOnlyList<Employee>> ListAsync(CancellationToken ct = default);
}
