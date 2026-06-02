using Dapper;
using SimpleNorthwind.Application.Abstractions.Persistence;
using SimpleNorthwind.Domain.Entities;

namespace SimpleNorthwind.Infrastructure.Repositories;

internal sealed class OrderDetailRepository(IUnitOfWork uow) : IOrderDetailRepository
{
    private const string Columns = "order_id, product_id, order_quantities, discount, version";

    public async Task InsertAsync(OrderDetail detail, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO dbo.order_details (order_id, product_id, order_quantities, discount, version)
            VALUES (@OrderId, @ProductId, @OrderQuantities, @Discount, @Version);
            """;
        await uow.Connection.ExecuteAsync(
            new CommandDefinition(sql, detail, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<OrderDetail>> ListByOrderAsync(int orderId, CancellationToken ct = default)
    {
        var sql = $"SELECT {Columns} FROM dbo.order_details WHERE order_id = @orderId ORDER BY product_id;";
        var rows = await uow.Connection.QueryAsync<OrderDetail>(
            new CommandDefinition(sql, new { orderId }, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
        return rows.ToList();
    }

    public async Task<IReadOnlyList<OrderDetail>> ListByOrdersAsync(IReadOnlyCollection<int> orderIds, CancellationToken ct = default)
    {
        if (orderIds.Count == 0)
            return [];

        var sql = $"SELECT {Columns} FROM dbo.order_details WHERE order_id IN @orderIds ORDER BY order_id, product_id;";
        var rows = await uow.Connection.QueryAsync<OrderDetail>(
            new CommandDefinition(sql, new { orderIds }, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
        return rows.ToList();
    }

    public async Task<bool> UpdateWithVersionAsync(OrderDetail detail, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE dbo.order_details
            SET order_quantities = @OrderQuantities, discount = @Discount, version = version + 1
            WHERE order_id = @OrderId AND product_id = @ProductId AND version = @Version;
            """;
        var affected = await uow.Connection.ExecuteAsync(
            new CommandDefinition(sql, detail, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
        return affected == 1;
    }

    public async Task DeleteAsync(int orderId, int productId, CancellationToken ct = default)
    {
        const string sql = "DELETE FROM dbo.order_details WHERE order_id = @orderId AND product_id = @productId;";
        await uow.Connection.ExecuteAsync(
            new CommandDefinition(sql, new { orderId, productId }, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
    }
}
