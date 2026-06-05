using Dapper;
using SimpleNorthwind.Application.Abstractions.Persistence;
using SimpleNorthwind.Application.Orders;
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

    // --- enrich 讀（JOIN 客戶 / 員工 / 修改者姓名） ---

    private static readonly string ViewSelect =
        $"""
        SELECT o.{Col(nameof(Order.OrderId))}, o.{Col(nameof(Order.CustomerId))}, c.{Col(nameof(Customer.CompanyName))} AS {Col(nameof(OrderHeaderRow.CustomerName))},
               o.{Col(nameof(Order.EmployeeId))}, (e.{Col(nameof(Employee.FirstName))} + ' ' + e.{Col(nameof(Employee.LastName))}) AS {Col(nameof(OrderHeaderRow.EmployeeName))},
               o.{Col(nameof(Order.OrderDate))}, o.{Col(nameof(Order.ModifiedEmployeeId))}, (m.{Col(nameof(Employee.FirstName))} + ' ' + m.{Col(nameof(Employee.LastName))}) AS {Col(nameof(OrderHeaderRow.ModifiedEmployeeName))},
               o.{Col(nameof(Order.ModifiedDate))}, o.{Col(nameof(Order.IsCanceled))}, o.{Col(nameof(Order.IsPaidoff))}
        FROM dbo.orders o
        JOIN dbo.customers c ON c.{Col(nameof(Customer.CustomerId))} = o.{Col(nameof(Order.CustomerId))}
        JOIN dbo.employees e ON e.{Col(nameof(Employee.EmployeeId))} = o.{Col(nameof(Order.EmployeeId))}
        LEFT JOIN dbo.employees m ON m.{Col(nameof(Employee.EmployeeId))} = o.{Col(nameof(Order.ModifiedEmployeeId))}
        """;

    private static readonly string GetViewSql = $"{ViewSelect} WHERE o.{Col(nameof(Order.OrderId))} = @orderId;";
    private static readonly string ListViewsSql = $"{ViewSelect} ORDER BY o.{Col(nameof(Order.OrderId))};";

    public async Task<OrderHeaderRow?> GetViewAsync(int orderId, CancellationToken ct = default) =>
        await uow.Connection.QuerySingleOrDefaultAsync<OrderHeaderRow>(
            new CommandDefinition(GetViewSql, new { orderId }, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);

    public async Task<IReadOnlyList<OrderHeaderRow>> ListViewsAsync(CancellationToken ct = default)
    {
        var rows = await uow.Connection.QueryAsync<OrderHeaderRow>(
            new CommandDefinition(ListViewsSql, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
        return rows.ToList();
    }
}
