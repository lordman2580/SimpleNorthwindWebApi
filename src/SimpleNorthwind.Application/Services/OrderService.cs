using SimpleNorthwind.Application.Abstractions.Persistence;
using SimpleNorthwind.Application.Abstractions.Services;
using SimpleNorthwind.Application.Orders;
using SimpleNorthwind.Domain.Common;
using SimpleNorthwind.Domain.Entities;

namespace SimpleNorthwind.Application.Services;

/// <summary>
/// 訂單 CRUD + 庫存扣減/還原 + 取消規則 + 樂觀並行。跨表寫入全在單一 UoW 交易內，
/// 多查詢一律依序 await（單一連線不可並行）。
/// </summary>
public sealed class OrderService(
    IUnitOfWork unitOfWork,
    IOrderRepository orders,
    IOrderDetailRepository orderDetails,
    IProductRepository products) : IOrderService
{
    public async Task<Result<OrderDto>> CreateAsync(CreateOrderRequest request, int actingEmployeeId, CancellationToken ct = default)
    {
        if (request.Details.Count == 0)
            return Error.Validation("order.empty", "訂單至少需一筆明細。");

        await unitOfWork.BeginAsync(ct).ConfigureAwait(false);
        try
        {
            // 依序扣庫存（不並行）：條件式 UPDATE 保證不超賣
            foreach (var detail in request.Details)
            {
                if (!await products.TryDecreaseStockAsync(detail.ProductId, detail.OrderQuantities, ct).ConfigureAwait(false))
                {
                    await unitOfWork.RollbackAsync(ct).ConfigureAwait(false);
                    return Error.Validation("order.insufficient_stock", $"產品 {detail.ProductId} 庫存不足或不存在。");
                }
            }

            var order = new Order
            {
                CustomerId = request.CustomerId,
                EmployeeId = actingEmployeeId,
                OrderDate = DateTime.UtcNow,
                IsCanceled = false,
                IsPaidoff = false
            };
            var orderId = await orders.InsertAsync(order, ct).ConfigureAwait(false);

            foreach (var detail in request.Details)
            {
                await orderDetails.InsertAsync(new OrderDetail
                {
                    OrderId = orderId,
                    ProductId = detail.ProductId,
                    OrderQuantities = detail.OrderQuantities,
                    Discount = detail.Discount,
                    Version = 1
                }, ct).ConfigureAwait(false);
            }

            await unitOfWork.CommitAsync(ct).ConfigureAwait(false);
            return await BuildDtoAsync(orderId, ct).ConfigureAwait(false);
        }
        catch
        {
            await unitOfWork.RollbackAsync(ct).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<Result<OrderDto>> UpdateAsync(int orderId, UpdateOrderRequest request, int actingEmployeeId, CancellationToken ct = default)
    {
        var order = await orders.GetByIdAsync(orderId, ct).ConfigureAwait(false);
        if (order is null)
            return Error.NotFound("order.not_found", $"找不到訂單 {orderId}。");
        if (order.IsCanceled)
            return Error.Conflict("order.canceled", "已取消的訂單不可修改。");
        if (request.Details.Count == 0)
            return Error.Validation("order.empty", "訂單至少需一筆明細。");

        await unitOfWork.BeginAsync(ct).ConfigureAwait(false);
        try
        {
            var current = (await orderDetails.ListByOrderAsync(orderId, ct).ConfigureAwait(false))
                .ToDictionary(d => d.ProductId);

            foreach (var requested in request.Details)
            {
                if (current.TryGetValue(requested.ProductId, out var existing))
                {
                    var delta = requested.OrderQuantities - existing.OrderQuantities;
                    if (delta > 0)
                    {
                        if (!await products.TryDecreaseStockAsync(requested.ProductId, delta, ct).ConfigureAwait(false))
                        {
                            await unitOfWork.RollbackAsync(ct).ConfigureAwait(false);
                            return Error.Validation("order.insufficient_stock", $"產品 {requested.ProductId} 庫存不足。");
                        }
                    }
                    else if (delta < 0)
                    {
                        await products.RestoreStockAsync(requested.ProductId, -delta, ct).ConfigureAwait(false);
                    }

                    var updated = new OrderDetail
                    {
                        OrderId = orderId,
                        ProductId = requested.ProductId,
                        OrderQuantities = requested.OrderQuantities,
                        Discount = requested.Discount,
                        Version = requested.Version
                    };
                    if (!await orderDetails.UpdateWithVersionAsync(updated, ct).ConfigureAwait(false))
                    {
                        await unitOfWork.RollbackAsync(ct).ConfigureAwait(false);
                        return Error.Conflict("order.version_conflict", $"產品 {requested.ProductId} 明細已被更新（版本衝突）。");
                    }
                }
                else
                {
                    if (!await products.TryDecreaseStockAsync(requested.ProductId, requested.OrderQuantities, ct).ConfigureAwait(false))
                    {
                        await unitOfWork.RollbackAsync(ct).ConfigureAwait(false);
                        return Error.Validation("order.insufficient_stock", $"產品 {requested.ProductId} 庫存不足或不存在。");
                    }
                    await orderDetails.InsertAsync(new OrderDetail
                    {
                        OrderId = orderId,
                        ProductId = requested.ProductId,
                        OrderQuantities = requested.OrderQuantities,
                        Discount = requested.Discount,
                        Version = 1
                    }, ct).ConfigureAwait(false);
                }
            }

            // 請求中不存在、但目前存在的明細 → 還原庫存後刪除
            var requestedIds = request.Details.Select(d => d.ProductId).ToHashSet();
            foreach (var (productId, existing) in current)
            {
                if (requestedIds.Contains(productId))
                    continue;
                await products.RestoreStockAsync(productId, existing.OrderQuantities, ct).ConfigureAwait(false);
                await orderDetails.DeleteAsync(orderId, productId, ct).ConfigureAwait(false);
            }

            await orders.TouchModifiedAsync(orderId, actingEmployeeId, DateTime.UtcNow, ct).ConfigureAwait(false);
            await unitOfWork.CommitAsync(ct).ConfigureAwait(false);
            return await BuildDtoAsync(orderId, ct).ConfigureAwait(false);
        }
        catch
        {
            await unitOfWork.RollbackAsync(ct).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<Result> CancelAsync(int orderId, int actingEmployeeId, CancellationToken ct = default)
    {
        var order = await orders.GetByIdAsync(orderId, ct).ConfigureAwait(false);
        if (order is null)
            return Result.Failure(Error.NotFound("order.not_found", $"找不到訂單 {orderId}。"));
        if (order.IsPaidoff)
            return Result.Failure(Error.Conflict("order.paidoff", "已付清的訂單不可取消。"));
        if (order.IsCanceled)
            return Result.Success();

        await unitOfWork.BeginAsync(ct).ConfigureAwait(false);
        try
        {
            var details = await orderDetails.ListByOrderAsync(orderId, ct).ConfigureAwait(false);
            foreach (var detail in details)
                await products.RestoreStockAsync(detail.ProductId, detail.OrderQuantities, ct).ConfigureAwait(false);

            await orders.SetCanceledAsync(orderId, actingEmployeeId, DateTime.UtcNow, ct).ConfigureAwait(false);
            await unitOfWork.CommitAsync(ct).ConfigureAwait(false);
            return Result.Success();
        }
        catch
        {
            await unitOfWork.RollbackAsync(ct).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<Result<OrderDto>> GetAsync(int orderId, CancellationToken ct = default)
    {
        var order = await orders.GetByIdAsync(orderId, ct).ConfigureAwait(false);
        if (order is null)
            return Error.NotFound("order.not_found", $"找不到訂單 {orderId}。");

        var details = await orderDetails.ListByOrderAsync(orderId, ct).ConfigureAwait(false);
        return MapOrder(order, details);
    }

    public async Task<IReadOnlyList<OrderDto>> ListAsync(CancellationToken ct = default)
    {
        var orderList = await orders.ListAsync(ct).ConfigureAwait(false);
        if (orderList.Count == 0)
            return [];

        var detailLookup = (await orderDetails
                .ListByOrdersAsync(orderList.Select(o => o.OrderId).ToList(), ct).ConfigureAwait(false))
            .GroupBy(d => d.OrderId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<OrderDetail>)g.ToList());

        return orderList
            .Select(o => MapOrder(o, detailLookup.TryGetValue(o.OrderId, out var details) ? details : []))
            .ToList();
    }

    private async Task<OrderDto> BuildDtoAsync(int orderId, CancellationToken ct)
    {
        var order = await orders.GetByIdAsync(orderId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"訂單 {orderId} 建立後找不到。");
        var details = await orderDetails.ListByOrderAsync(orderId, ct).ConfigureAwait(false);
        return MapOrder(order, details);
    }

    private static OrderDto MapOrder(Order order, IReadOnlyList<OrderDetail> details) =>
        new(order.OrderId, order.CustomerId, order.EmployeeId, order.OrderDate,
            order.ModifiedEmployeeId, order.ModifiedDate, order.IsCanceled, order.IsPaidoff,
            details.Select(d => new OrderDetailDto(d.ProductId, d.OrderQuantities, d.Discount, d.Version)).ToList());
}
