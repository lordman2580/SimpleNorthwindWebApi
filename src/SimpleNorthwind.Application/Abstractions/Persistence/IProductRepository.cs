using SimpleNorthwind.Application.Common;
using SimpleNorthwind.Application.Products;

namespace SimpleNorthwind.Application.Abstractions.Persistence;

public interface IProductRepository
{
    /// <summary>
    /// 條件式扣庫存：UPDATE ... SET quantities = quantities - @qty WHERE product_id=@id AND quantities >= @qty。
    /// 受影響列數 = 1 → true；0（庫存不足或產品不存在）→ false。不超賣的核心保證。
    /// </summary>
    Task<bool> TryDecreaseStockAsync(int productId, int quantity, CancellationToken ct = default);

    /// <summary>還原庫存：UPDATE ... SET quantities = quantities + @qty WHERE product_id=@id。</summary>
    Task RestoreStockAsync(int productId, int quantity, CancellationToken ct = default);

    /// <summary>分頁 + 類別過濾 + 排序的產品清單（JOIN categories 取分類名）。sortBy ∈ {name,category,price,stock}。</summary>
    Task<PagedResult<ProductDto>> ListAsync(int page, int pageSize, string? category, string? sortBy, bool desc, CancellationToken ct = default);

    /// <summary>低庫存清單（quantities &lt;= 門檻），供 Dashboard 庫存預警。</summary>
    Task<IReadOnlyList<ProductDto>> ListLowStockAsync(int threshold, int take, CancellationToken ct = default);
}
