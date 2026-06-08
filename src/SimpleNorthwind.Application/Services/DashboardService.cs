using SimpleNorthwind.Application.Abstractions.Persistence;
using SimpleNorthwind.Application.Abstractions.Services;
using SimpleNorthwind.Application.ApiLogs;
using SimpleNorthwind.Application.Dashboard;
using SimpleNorthwind.Application.Orders;

namespace SimpleNorthwind.Application.Services;

/// <summary>
/// 總覽彙總（精簡版）。所有 KPI / 清單由後端計算（業務處理在後端，前端只呈現）。
/// 共用 UoW 連線的查詢一律依序 await（單一連線不可並行）；稽核計數走獨立連線。
/// </summary>
public sealed class DashboardService(
    IOrderService orders,
    ICustomerService customers,
    IProductRepository products,
    IApiLogRepository apiLogs) : IDashboardService
{
    private const int RecentTake = 6;
    private const int LowStockTake = 5;

    public async Task<DashboardSummaryDto> GetSummaryAsync(int lowStockThreshold, CancellationToken ct = default)
    {
        // UoW 單一連線 → 依序 await（不並行）
        var allOrders = await orders.ListAsync(ct).ConfigureAwait(false);
        var allCustomers = await customers.ListAsync(ct).ConfigureAwait(false);
        var lowStock = await products.ListLowStockAsync(lowStockThreshold, LowStockTake, ct).ConfigureAwait(false);

        // 稽核計數（今日 UTC）— ApiLogRepository 用獨立短連線
        var todayStart = DateTime.UtcNow.Date;
        var todayEnd = todayStart.AddDays(1);
        var auditTotal = (await apiLogs.QueryAsync(new ApiLogQuery(null, null, false, todayStart, todayEnd, 1, 1), ct).ConfigureAwait(false)).TotalCount;
        var auditErrors = (await apiLogs.QueryAsync(new ApiLogQuery(null, null, true, todayStart, todayEnd, 1, 1), ct).ConfigureAwait(false)).TotalCount;

        // 單筆訂單折扣後合計（折扣為百分比 0..100）。已取消訂單不計入任何營收。
        static decimal OrderTotal(OrderDto o) =>
            o.Details.Sum(d => d.UnitPrice * d.OrderQuantities * (1 - d.Discount / 100m));

        var settledRevenue = allOrders.Where(o => !o.IsCanceled && o.IsPaidoff).Sum(OrderTotal);   // 實際營收（已結清）
        var expectedRevenue = allOrders.Where(o => !o.IsCanceled && !o.IsPaidoff).Sum(OrderTotal);  // 預計營收（未結清）
        var openCount = allOrders.Count(o => !o.IsCanceled && !o.IsPaidoff);
        var recent = allOrders.Where(o => !o.IsCanceled).OrderByDescending(o => o.OrderId).Take(RecentTake).ToList();

        return new DashboardSummaryDto(
            allOrders.Count, allCustomers.Count, openCount, settledRevenue, expectedRevenue,
            recent, lowStock, auditTotal, auditErrors);
    }
}
