using SimpleNorthwind.Application.Orders;
using SimpleNorthwind.Domain.Entities;

namespace SimpleNorthwind.Application.Abstractions.Persistence;

public interface IOrderRepository
{
    /// <summary>新增訂單並回傳新 order_id。</summary>
    Task<int> InsertAsync(Order order, CancellationToken ct = default);

    Task<Order?> GetByIdAsync(int orderId, CancellationToken ct = default);
    Task<IReadOnlyList<Order>> ListAsync(CancellationToken ct = default);

    /// <summary>enrich 讀：單筆訂單表頭含客戶 / 員工 / 修改者姓名（JOIN）。供 GET 端點 / DTO 組裝。</summary>
    Task<OrderHeaderRow?> GetViewAsync(int orderId, CancellationToken ct = default);

    /// <summary>enrich 讀：所有訂單表頭含名稱（JOIN，單一 round-trip，無 N+1）。</summary>
    Task<IReadOnlyList<OrderHeaderRow>> ListViewsAsync(CancellationToken ct = default);

    /// <summary>標記取消（is_canceled=1）並寫 modified_*；受影響列數 &gt; 0 回 true。</summary>
    Task<bool> SetCanceledAsync(int orderId, int modifiedEmployeeId, DateTime modifiedDateUtc, CancellationToken ct = default);

    /// <summary>更新 modified_employee_id / modified_date。</summary>
    Task TouchModifiedAsync(int orderId, int modifiedEmployeeId, DateTime modifiedDateUtc, CancellationToken ct = default);
}
