using SimpleNorthwind.Domain.Entities;

namespace SimpleNorthwind.Application.Abstractions.Persistence;

public interface IOrderDetailRepository
{
    Task InsertAsync(OrderDetail detail, CancellationToken ct = default);

    Task<IReadOnlyList<OrderDetail>> ListByOrderAsync(int orderId, CancellationToken ct = default);

    /// <summary>一次取多張訂單的明細（避免 List 訂單時 N+1）。</summary>
    Task<IReadOnlyList<OrderDetail>> ListByOrdersAsync(IReadOnlyCollection<int> orderIds, CancellationToken ct = default);

    /// <summary>
    /// 樂觀並行更新：UPDATE ... SET ..., version = version + 1 WHERE order_id=@o AND product_id=@p AND version=@v。
    /// 受影響列數 = 1 → true；0（版本衝突）→ false。
    /// </summary>
    Task<bool> UpdateWithVersionAsync(OrderDetail detail, CancellationToken ct = default);

    Task DeleteAsync(int orderId, int productId, CancellationToken ct = default);
}
