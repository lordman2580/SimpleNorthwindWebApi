using Dapper;
using SimpleNorthwind.Application.Abstractions.Persistence;
using SimpleNorthwind.Domain.Entities;
using static SimpleNorthwind.Infrastructure.Persistence.SqlNaming;

namespace SimpleNorthwind.Infrastructure.Repositories;

internal sealed class ProductRepository(IUnitOfWork uow) : IProductRepository
{
    // 條件式扣庫存：quantities >= @quantity 才扣，不足回 0 列（不超賣）。
    private static readonly string DecreaseSql =
        $"""
        UPDATE dbo.products
        SET {Col(nameof(Product.Quantities))} = {Col(nameof(Product.Quantities))} - @quantity
        WHERE {Col(nameof(Product.ProductId))} = @productId AND {Col(nameof(Product.Quantities))} >= @quantity;
        """;

    private static readonly string RestoreSql =
        $"UPDATE dbo.products SET {Col(nameof(Product.Quantities))} = {Col(nameof(Product.Quantities))} + @quantity " +
        $"WHERE {Col(nameof(Product.ProductId))} = @productId;";

    public async Task<bool> TryDecreaseStockAsync(int productId, int quantity, CancellationToken ct = default)
    {
        var affected = await uow.Connection.ExecuteAsync(
            new CommandDefinition(DecreaseSql, new { productId, quantity }, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
        return affected == 1;
    }

    public async Task RestoreStockAsync(int productId, int quantity, CancellationToken ct = default) =>
        await uow.Connection.ExecuteAsync(
            new CommandDefinition(RestoreSql, new { productId, quantity }, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
}
