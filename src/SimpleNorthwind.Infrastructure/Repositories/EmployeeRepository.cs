using Dapper;
using SimpleNorthwind.Application.Abstractions.Persistence;
using SimpleNorthwind.Domain.Entities;
using SimpleNorthwind.Infrastructure.Persistence;
using static SimpleNorthwind.Infrastructure.Persistence.SqlNaming;

namespace SimpleNorthwind.Infrastructure.Repositories;

internal sealed class EmployeeRepository(IUnitOfWork uow) : IEmployeeRepository
{
    private static readonly string GetByIdSql =
        $"SELECT {EntityColumns<Employee>.All} FROM dbo.employees WHERE {Col(nameof(Employee.EmployeeId))} = @employeeId;";

    public async Task<Employee?> GetByIdAsync(int employeeId, CancellationToken ct = default) =>
        await uow.Connection.QuerySingleOrDefaultAsync<Employee>(
            new CommandDefinition(GetByIdSql, new { employeeId }, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
}
