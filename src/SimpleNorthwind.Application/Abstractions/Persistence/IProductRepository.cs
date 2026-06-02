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
}
