using Dapper;
using SimpleNorthwind.Application.Abstractions.Persistence;
using SimpleNorthwind.Domain.Entities;

namespace SimpleNorthwind.Infrastructure.Repositories;

internal sealed class OrderRepository(IUnitOfWork uow) : IOrderRepository
{
    private const string Columns =
        "order_id, customer_id, employee_id, order_date, modified_employee_id, modified_date, is_canceled, is_paidoff";

    public async Task<int> InsertAsync(Order order, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO dbo.orders (customer_id, employee_id, order_date, modified_employee_id, modified_date, is_canceled, is_paidoff)
            OUTPUT INSERTED.order_id
            VALUES (@CustomerId, @EmployeeId, @OrderDate, @ModifiedEmployeeId, @ModifiedDate, @IsCanceled, @IsPaidoff);
            """;
        return await uow.Connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, order, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
    }

    public async Task<Order?> GetByIdAsync(int orderId, CancellationToken ct = default)
    {
        var sql = $"SELECT {Columns} FROM dbo.orders WHERE order_id = @orderId;";
        return await uow.Connection.QuerySingleOrDefaultAsync<Order>(
            new CommandDefinition(sql, new { orderId }, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Order>> ListAsync(CancellationToken ct = default)
    {
        var sql = $"SELECT {Columns} FROM dbo.orders ORDER BY order_id;";
        var rows = await uow.Connection.QueryAsync<Order>(
            new CommandDefinition(sql, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
        return rows.ToList();
    }

    public async Task<bool> SetCanceledAsync(int orderId, int modifiedEmployeeId, DateTime modifiedDateUtc, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE dbo.orders
            SET is_canceled = 1, modified_employee_id = @modifiedEmployeeId, modified_date = @modifiedDateUtc
            WHERE order_id = @orderId;
            """;
        var affected = await uow.Connection.ExecuteAsync(
            new CommandDefinition(sql, new { orderId, modifiedEmployeeId, modifiedDateUtc }, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
        return affected > 0;
    }

    public async Task TouchModifiedAsync(int orderId, int modifiedEmployeeId, DateTime modifiedDateUtc, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE dbo.orders
            SET modified_employee_id = @modifiedEmployeeId, modified_date = @modifiedDateUtc
            WHERE order_id = @orderId;
            """;
        await uow.Connection.ExecuteAsync(
            new CommandDefinition(sql, new { orderId, modifiedEmployeeId, modifiedDateUtc }, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
    }
}
