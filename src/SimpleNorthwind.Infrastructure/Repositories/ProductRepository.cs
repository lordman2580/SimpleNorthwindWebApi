using Dapper;
using SimpleNorthwind.Application.Abstractions.Persistence;

namespace SimpleNorthwind.Infrastructure.Repositories;

internal sealed class ProductRepository(IUnitOfWork uow) : IProductRepository
{
    public async Task<bool> TryDecreaseStockAsync(int productId, int quantity, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE dbo.products
            SET quantities = quantities - @quantity
            WHERE product_id = @productId AND quantities >= @quantity;
            """;
        var affected = await uow.Connection.ExecuteAsync(
            new CommandDefinition(sql, new { productId, quantity }, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
        return affected == 1;
    }

    public async Task RestoreStockAsync(int productId, int quantity, CancellationToken ct = default)
    {
        const string sql = "UPDATE dbo.products SET quantities = quantities + @quantity WHERE product_id = @productId;";
        await uow.Connection.ExecuteAsync(
            new CommandDefinition(sql, new { productId, quantity }, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
    }
}
