using SimpleNorthwind.Domain.Entities;

namespace SimpleNorthwind.Application.Abstractions.Persistence;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(int customerId, CancellationToken ct = default);
    Task<IReadOnlyList<Customer>> ListAsync(CancellationToken ct = default);

    /// <summary>新增並回傳新 customer_id。</summary>
    Task<int> InsertAsync(Customer customer, CancellationToken ct = default);

    /// <summary>更新；受影響列數 &gt; 0 回 true。</summary>
    Task<bool> UpdateAsync(Customer customer, CancellationToken ct = default);

    /// <summary>硬刪除；受影響列數 &gt; 0 回 true。</summary>
    Task<bool> DeleteAsync(int customerId, CancellationToken ct = default);
}
