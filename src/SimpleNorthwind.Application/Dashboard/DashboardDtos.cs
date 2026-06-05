using SimpleNorthwind.Application.Orders;
using SimpleNorthwind.Application.Products;

namespace SimpleNorthwind.Application.Dashboard;

/// <summary>
/// 總覽（Dashboard）彙總（精簡版，UD10）。全為後端計算；前端只呈現。
/// 期間比較 delta% 因無歷史快照不做。
/// </summary>
public sealed record DashboardSummaryDto(
    int OrderCount,
    int CustomerCount,
    int OpenOrderCount,
    decimal Revenue,
    IReadOnlyList<OrderDto> RecentOrders,
    IReadOnlyList<ProductDto> LowStock,
    int AuditTotalToday,
    int AuditErrorToday);
