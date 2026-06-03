using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SimpleNorthwind.Application.Abstractions.Services;
using SimpleNorthwind.Application.Dashboard;
using SimpleNorthwind.Infrastructure.Options;

namespace SimpleNorthwind.WebApi.Controllers;

/// <summary>總覽（Dashboard）彙總。需 JWT。低庫存門檻取自 <c>App:LowStockThreshold</c>。</summary>
[ApiController]
[Route("api/dashboard")]
[Authorize]
public sealed class DashboardController(IDashboardService dashboardService, IOptions<AppOptions> appOptions) : ControllerBase
{
    /// <summary>取得總覽彙總（KPI / 最新訂單 / 庫存預警 / 稽核計數）。</summary>
    /// <param name="ct">取消權杖。</param>
    /// <response code="200">回傳總覽彙總。</response>
    /// <response code="401">未提供或無效的 JWT。</response>
    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryDto>> Summary(CancellationToken ct)
        => Ok(await dashboardService.GetSummaryAsync(appOptions.Value.LowStockThreshold, ct));
}
