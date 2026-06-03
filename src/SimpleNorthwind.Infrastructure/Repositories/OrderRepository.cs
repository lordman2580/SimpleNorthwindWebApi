using Dapper;
using SimpleNorthwind.Application.Abstractions.Persistence;
using SimpleNorthwind.Domain.Entities;
using SimpleNorthwind.Infrastructure.Persistence;
using static SimpleNorthwind.Infrastructure.Persistence.SqlNaming;

namespace SimpleNorthwind.Infrastructure.Repositories;

internal sealed class OrderRepository(IUnitOfWork uow) : IOrderRepository
{
    private static readonly string Columns = EntityColumns<Order>.All;

    // 寫入欄位（排除 IDENTITY 主鍵 OrderId）。
    private static readonly string[] InsertProps =
    [
        nameof(Order.CustomerId), nameof(Order.EmployeeId), nameof(Order.OrderDate),
        nameof(Order.ModifiedEmployeeId), nameof(Order.ModifiedDate), nameof(Order.IsCanceled), nameof(Order.IsPaidoff),
    ];

    private static readonly string InsertSql =
        $"""
        INSERT INTO dbo.orders ({Cols(InsertProps)})
        OUTPUT INSERTED.{Col(nameof(Order.OrderId))}
        VALUES ({Params(InsertProps)});
        """;

    private static readonly string GetByIdSql =
        $"SELECT {Columns} FROM dbo.orders WHERE {Col(nameof(Order.OrderId))} = @orderId;";

    private static readonly string ListSql =
        $"SELECT {Columns} FROM dbo.orders ORDER BY {Col(nameof(Order.OrderId))};";

    private static readonly string SetCanceledSql =
        $"""
        UPDATE dbo.orders
        SET {Col(nameof(Order.IsCanceled))} = 1,
            {Col(nameof(Order.ModifiedEmployeeId))} = @modifiedEmployeeId,
            {Col(nameof(Order.ModifiedDate))} = @modifiedDateUtc
        WHERE {Col(nameof(Order.OrderId))} = @orderId;
        """;

    private static readonly string TouchModifiedSql =
        $"""
        UPDATE dbo.orders
        SET {Col(nameof(Order.ModifiedEmployeeId))} = @modifiedEmployeeId,
            {Col(nameof(Order.ModifiedDate))} = @modifiedDateUtc
        WHERE {Col(nameof(Order.OrderId))} = @orderId;
        """;

    public async Task<int> InsertAsync(Order order, CancellationToken ct = default) =>
        await uow.Connection.ExecuteScalarAsync<int>(
            new CommandDefinition(InsertSql, order, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);

    public async Task<Order?> GetByIdAsync(int orderId, CancellationToken ct = default) =>
        await uow.Connection.QuerySingleOrDefaultAsync<Order>(
            new CommandDefinition(GetByIdSql, new { orderId }, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);

    public async Task<IReadOnlyList<Order>> ListAsync(CancellationToken ct = default)
    {
        var rows = await uow.Connection.QueryAsync<Order>(
            new CommandDefinition(ListSql, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
        return rows.ToList();
    }

    public async Task<bool> SetCanceledAsync(int orderId, int modifiedEmployeeId, DateTime modifiedDateUtc, CancellationToken ct = default)
    {
        var affected = await uow.Connection.ExecuteAsync(
            new CommandDefinition(SetCanceledSql, new { orderId, modifiedEmployeeId, modifiedDateUtc }, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
        return affected > 0;
    }

    public async Task TouchModifiedAsync(int orderId, int modifiedEmployeeId, DateTime modifiedDateUtc, CancellationToken ct = default) =>
        await uow.Connection.ExecuteAsync(
            new CommandDefinition(TouchModifiedSql, new { orderId, modifiedEmployeeId, modifiedDateUtc }, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
}
