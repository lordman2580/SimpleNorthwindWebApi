using Dapper;
using SimpleNorthwind.Application.Abstractions.Persistence;
using SimpleNorthwind.Application.Orders;
using SimpleNorthwind.Domain.Entities;
using SimpleNorthwind.Infrastructure.Persistence;
using static SimpleNorthwind.Infrastructure.Persistence.SqlNaming;

namespace SimpleNorthwind.Infrastructure.Repositories;

internal sealed class OrderDetailRepository(IUnitOfWork uow) : IOrderDetailRepository
{
    private static readonly string Columns = EntityColumns<OrderDetail>.All;

    private static readonly string[] InsertProps =
    [
        nameof(OrderDetail.OrderId), nameof(OrderDetail.ProductId), nameof(OrderDetail.OrderQuantities),
        nameof(OrderDetail.Discount), nameof(OrderDetail.Version),
    ];

    private static readonly string InsertSql =
        $"INSERT INTO dbo.order_details ({Cols(InsertProps)}) VALUES ({Params(InsertProps)});";

    private static readonly string ListByOrderSql =
        $"SELECT {Columns} FROM dbo.order_details WHERE {Col(nameof(OrderDetail.OrderId))} = @orderId " +
        $"ORDER BY {Col(nameof(OrderDetail.ProductId))};";

    private static readonly string ListByOrdersSql =
        $"SELECT {Columns} FROM dbo.order_details WHERE {Col(nameof(OrderDetail.OrderId))} IN @orderIds " +
        $"ORDER BY {Col(nameof(OrderDetail.OrderId))}, {Col(nameof(OrderDetail.ProductId))};";

    // 樂觀並行：WHERE 比對 client 帶回的 version，符合才更新並自增；不符 → 0 列 → 版本衝突。
    private static readonly string UpdateWithVersionSql =
        $"""
        UPDATE dbo.order_details
        SET {Col(nameof(OrderDetail.OrderQuantities))} = @{nameof(OrderDetail.OrderQuantities)},
            {Col(nameof(OrderDetail.Discount))} = @{nameof(OrderDetail.Discount)},
            {Col(nameof(OrderDetail.Version))} = {Col(nameof(OrderDetail.Version))} + 1
        WHERE {Col(nameof(OrderDetail.OrderId))} = @{nameof(OrderDetail.OrderId)}
          AND {Col(nameof(OrderDetail.ProductId))} = @{nameof(OrderDetail.ProductId)}
          AND {Col(nameof(OrderDetail.Version))} = @{nameof(OrderDetail.Version)};
        """;

    private static readonly string DeleteSql =
        $"DELETE FROM dbo.order_details WHERE {Col(nameof(OrderDetail.OrderId))} = @orderId " +
        $"AND {Col(nameof(OrderDetail.ProductId))} = @productId;";

    public async Task InsertAsync(OrderDetail detail, CancellationToken ct = default) =>
        await uow.Connection.ExecuteAsync(
            new CommandDefinition(InsertSql, detail, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);

    public async Task<IReadOnlyList<OrderDetail>> ListByOrderAsync(int orderId, CancellationToken ct = default)
    {
        var rows = await uow.Connection.QueryAsync<OrderDetail>(
            new CommandDefinition(ListByOrderSql, new { orderId }, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
        return rows.ToList();
    }

    public async Task<IReadOnlyList<OrderDetail>> ListByOrdersAsync(IReadOnlyCollection<int> orderIds, CancellationToken ct = default)
    {
        if (orderIds.Count == 0)
            return [];

        var rows = await uow.Connection.QueryAsync<OrderDetail>(
            new CommandDefinition(ListByOrdersSql, new { orderIds }, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
        return rows.ToList();
    }

    public async Task<bool> UpdateWithVersionAsync(OrderDetail detail, CancellationToken ct = default)
    {
        var affected = await uow.Connection.ExecuteAsync(
            new CommandDefinition(UpdateWithVersionSql, detail, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
        return affected == 1;
    }

    public async Task DeleteAsync(int orderId, int productId, CancellationToken ct = default) =>
        await uow.Connection.ExecuteAsync(
            new CommandDefinition(DeleteSql, new { orderId, productId }, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);

    // enrich 讀：明細 JOIN products 取名稱 / 單價。
    private static readonly string ListRowsByOrdersSql =
        $"""
        SELECT od.{Col(nameof(OrderDetail.OrderId))}, od.{Col(nameof(OrderDetail.ProductId))},
               p.{Col(nameof(Product.ProductName))}, p.{Col(nameof(Product.UnitPrice))},
               od.{Col(nameof(OrderDetail.OrderQuantities))}, od.{Col(nameof(OrderDetail.Discount))}, od.{Col(nameof(OrderDetail.Version))}
        FROM dbo.order_details od
        JOIN dbo.products p ON p.{Col(nameof(Product.ProductId))} = od.{Col(nameof(OrderDetail.ProductId))}
        WHERE od.{Col(nameof(OrderDetail.OrderId))} IN @orderIds
        ORDER BY od.{Col(nameof(OrderDetail.OrderId))}, od.{Col(nameof(OrderDetail.ProductId))};
        """;

    public async Task<IReadOnlyList<OrderDetailRow>> ListRowsByOrdersAsync(IReadOnlyCollection<int> orderIds, CancellationToken ct = default)
    {
        if (orderIds.Count == 0)
            return [];

        var rows = await uow.Connection.QueryAsync<OrderDetailRow>(
            new CommandDefinition(ListRowsByOrdersSql, new { orderIds }, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
        return rows.ToList();
    }
}
