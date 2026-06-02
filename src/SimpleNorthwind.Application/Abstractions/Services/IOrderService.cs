using SimpleNorthwind.Application.Orders;
using SimpleNorthwind.Domain.Common;

namespace SimpleNorthwind.Application.Abstractions.Services;

public interface IOrderService
{
    Task<Result<OrderDto>> CreateAsync(CreateOrderRequest request, int actingEmployeeId, CancellationToken ct = default);
    Task<Result<OrderDto>> UpdateAsync(int orderId, UpdateOrderRequest request, int actingEmployeeId, CancellationToken ct = default);
    Task<Result> CancelAsync(int orderId, int actingEmployeeId, CancellationToken ct = default);
    Task<Result<OrderDto>> GetAsync(int orderId, CancellationToken ct = default);
    Task<IReadOnlyList<OrderDto>> ListAsync(CancellationToken ct = default);
}
