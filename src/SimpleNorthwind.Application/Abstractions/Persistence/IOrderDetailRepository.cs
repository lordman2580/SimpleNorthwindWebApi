using SimpleNorthwind.Domain.Entities;

namespace SimpleNorthwind.Application.Abstractions.Persistence;

public interface IOrderDetailRepository
{
    Task InsertAsync(OrderDetail detail, CancellationToken ct = default);

    Task<IReadOnlyList<OrderDetail>> ListByOrderAsync(int orderId, CancellationToken ct = default);

    /// <summary>一次取多張訂單的明細（避免 List 訂單時 N+1）。</summary>
    Task<IReadOnlyList<OrderDetail>> ListByOrdersAsync(IReadOnlyCollection<int> orderIds, CancellationToken ct = default);

    /// <summary>
    /// 更新明細數量 / 折扣，並將 version 自增（version 由伺服器端管理，不接受使用者帶入）：
    /// UPDATE ... SET ..., version = version + 1 WHERE order_id=@o AND product_id=@p。受影響列數 = 1 → true。
    /// </summary>
    Task<bool> UpdateAsync(OrderDetail detail, CancellationToken ct = default);

    Task DeleteAsync(int orderId, int productId, CancellationToken ct = default);
}
