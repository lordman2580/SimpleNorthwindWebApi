using Dapper;
using SimpleNorthwind.Application.Abstractions.Persistence;
using SimpleNorthwind.Application.Common;
using SimpleNorthwind.Application.Products;
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

    // 排序欄白名單：欄名不可參數化，只允許白名單值（防 SQL injection）。
    private static readonly IReadOnlyDictionary<string, string> SortColumns =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["name"] = $"p.{Col(nameof(Product.ProductName))}",
            ["category"] = $"c.{Col(nameof(Category.CategoryName))}",
            ["price"] = $"p.{Col(nameof(Product.UnitPrice))}",
            ["stock"] = $"p.{Col(nameof(Product.Quantities))}",
        };

    private static readonly string SelectCols =
        $"p.{Col(nameof(Product.ProductId))}, p.{Col(nameof(Product.ProductName))}, " +
        $"c.{Col(nameof(Category.CategoryName))}, p.{Col(nameof(Product.Quantities))}, p.{Col(nameof(Product.UnitPrice))}";

    private static readonly string FromWhere =
        $"""
        FROM dbo.products p
        JOIN dbo.categories c ON c.{Col(nameof(Category.CategoryId))} = p.{Col(nameof(Product.CategoryId))}
        WHERE (@category IS NULL OR c.{Col(nameof(Category.CategoryName))} = @category)
        """;

    private static readonly string CountSql = $"SELECT COUNT(*) {FromWhere};";

    private static readonly string LowStockSql =
        $"""
        SELECT TOP (@take) {SelectCols}
        FROM dbo.products p
        JOIN dbo.categories c ON c.{Col(nameof(Category.CategoryId))} = p.{Col(nameof(Product.CategoryId))}
        WHERE p.{Col(nameof(Product.Quantities))} <= @threshold
        ORDER BY p.{Col(nameof(Product.Quantities))} ASC;
        """;

    public async Task<bool> TryDecreaseStockAsync(int productId, int quantity, CancellationToken ct = default)
    {
        var affected = await uow.Connection.ExecuteAsync(
            new CommandDefinition(DecreaseSql, new { productId, quantity }, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
        return affected == 1;
    }

    public async Task RestoreStockAsync(int productId, int quantity, CancellationToken ct = default) =>
        await uow.Connection.ExecuteAsync(
            new CommandDefinition(RestoreSql, new { productId, quantity }, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);

    public async Task<PagedResult<ProductDto>> ListAsync(int page, int pageSize, string? category, string? sortBy, bool desc, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 10 : pageSize;
        var orderCol = SortColumns.TryGetValue(sortBy ?? "name", out var col) ? col : SortColumns["name"];
        var dir = desc ? "DESC" : "ASC";

        var pageSql =
            $"SELECT {SelectCols} {FromWhere} ORDER BY {orderCol} {dir} " +
            "OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;";

        var args = new { category, offset = (page - 1) * pageSize, pageSize };

        // 單一連線不可並行 → 依序 await（先 count 再 page）。
        var total = await uow.Connection.ExecuteScalarAsync<int>(
            new CommandDefinition(CountSql, args, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
        var items = (await uow.Connection.QueryAsync<ProductDto>(
            new CommandDefinition(pageSql, args, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false)).ToList();
        return new PagedResult<ProductDto>(items, page, pageSize, total);
    }

    public async Task<IReadOnlyList<ProductDto>> ListLowStockAsync(int threshold, int take, CancellationToken ct = default)
    {
        var rows = await uow.Connection.QueryAsync<ProductDto>(
            new CommandDefinition(LowStockSql, new { threshold, take }, transaction: uow.Transaction, cancellationToken: ct)).ConfigureAwait(false);
        return rows.ToList();
    }
}
