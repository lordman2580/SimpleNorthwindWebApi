using Dapper;
using SimpleNorthwind.Application.Abstractions.Persistence;
using SimpleNorthwind.Domain.Entities;

namespace SimpleNorthwind.Infrastructure.Repositories;

internal sealed class EmployeeRepository(IUnitOfWork uow) : IEmployeeRepository
{
    public async Task<Employee?> GetByIdAsync(int employeeId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT employee_id, password, last_name, first_name, title, birth_date, hire_date,
                   phone_ext_number, phone_number, notes, is_resigned, resign_date
            FROM dbo.employees
            WHERE employee_id = @employeeId;
            """;
        return await uow.Connection.QuerySingleOrDefaultAsync<Employee>(
            new CommandDefinition(sql, new { employeeId }, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
    }
}
