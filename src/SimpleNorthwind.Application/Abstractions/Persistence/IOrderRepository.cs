using SimpleNorthwind.Domain.Entities;

namespace SimpleNorthwind.Application.Abstractions.Persistence;

public interface IOrderRepository
{
    /// <summary>新增訂單並回傳新 order_id。</summary>
    Task<int> InsertAsync(Order order, CancellationToken ct = default);

    Task<Order?> GetByIdAsync(int orderId, CancellationToken ct = default);
    Task<IReadOnlyList<Order>> ListAsync(CancellationToken ct = default);

    /// <summary>標記取消（is_canceled=1）並寫 modified_*；受影響列數 &gt; 0 回 true。</summary>
    Task<bool> SetCanceledAsync(int orderId, int modifiedEmployeeId, DateTime modifiedDateUtc, CancellationToken ct = default);

    /// <summary>更新 modified_employee_id / modified_date。</summary>
    Task TouchModifiedAsync(int orderId, int modifiedEmployeeId, DateTime modifiedDateUtc, CancellationToken ct = default);
}
