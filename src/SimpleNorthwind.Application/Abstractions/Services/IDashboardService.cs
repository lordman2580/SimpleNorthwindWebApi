using SimpleNorthwind.Application.Dashboard;

namespace SimpleNorthwind.Application.Abstractions.Services;

public interface IDashboardService
{
    /// <summary>總覽彙總（KPI / 最新訂單 / 低庫存 / 稽核計數）。低庫存門檻由呼叫端帶入（App:LowStockThreshold）。</summary>
    Task<DashboardSummaryDto> GetSummaryAsync(int lowStockThreshold, CancellationToken ct = default);
}
