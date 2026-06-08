using System.Net;
using Microsoft.AspNetCore.Mvc;
using SimpleNorthwind.Application.Dashboard;
using SimpleNorthwind.WebApi.Web.Extensions;
using SimpleNorthwind.WebApi.Web.Http;
using SimpleNorthwind.WebApi.Web.Models;

namespace SimpleNorthwind.WebApi.Web.Controllers;

/// <summary>
/// 首頁 / 總覽（Dashboard，UI，Cookie scheme 由 <see cref="UiControllerBase"/> 提供）。
/// 未登入存取 → Cookie handler 導向 /account/login。
/// 經 loopback 呼叫 <c>/api/dashboard/summary</c> 取得彙總資料並映射為 <see cref="DashboardViewModel"/>。
/// </summary>
public sealed class HomeUiController(NorthwindApiClient apiClient) : UiControllerBase
{
    /// <summary>
    /// 總覽頁。維持 <c>[HttpGet("/")]</c> 使根路徑映射至此動作。
    /// </summary>
    [HttpGet("/")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var result = await apiClient.GetDashboardSummaryAsync(ct);
        if (await RedirectToLoginIfUnauthorizedAsync(result.StatusCode) is { } redirect)
            return redirect;

        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Error"] = result.Detail ?? "總覽資料載入失敗。";
            return View("~/Web/Views/Home/Index.cshtml", new DashboardViewModel { LoadFailed = true });
        }

        var vm = MapToViewModel(result.Value);
        return View("~/Web/Views/Home/Index.cshtml", vm);
    }

    /// <summary>
    /// 即時刷新用：回傳 dashboard 資料區塊 partial（供 <c>dashboard-live.js</c> 收到稽核事件後重抓換新）。
    /// 未授權回 401（前端自行導向登入，不走 302 以免把登入頁塞進區塊）；失敗回 500（前端保留現況不換）。
    /// </summary>
    [HttpGet("/dashboard/body")]
    public async Task<IActionResult> Body(CancellationToken ct)
    {
        var result = await apiClient.GetDashboardSummaryAsync(ct);
        if (result.StatusCode == HttpStatusCode.Unauthorized)
            return Unauthorized();
        if (!result.IsSuccess || result.Value is null)
            return StatusCode(StatusCodes.Status500InternalServerError);

        return PartialView("~/Web/Views/Home/_DashboardBody.cshtml", MapToViewModel(result.Value));
    }

    /// <summary>
    /// 將後端彙總 DTO 映射為 View 檢視模型。
    /// 最新訂單金額 = Σ(單價 × 數量 × (1 − 折扣百分比 / 100))；折扣以百分比表示。
    /// 庫存狀態：數量為 0 → 缺貨，否則 → 低庫存。
    /// </summary>
    private static DashboardViewModel MapToViewModel(DashboardSummaryDto summary)
    {
        var recentOrders = summary.RecentOrders
            .Select(o => new DashboardOrderRow
            {
                OrderId = o.OrderId,
                CustomerName = o.CustomerName,
                EmployeeName = o.EmployeeName,
                OrderDate = o.OrderDate,
                Status = o.Status,
                Total = o.Total(),
            })
            .ToList();

        var lowStock = summary.LowStock
            .Select(p => new DashboardStockRow
            {
                ProductName = p.ProductName,
                CategoryName = p.CategoryName,
                Quantities = p.Quantities,
                StockStatus = p.Quantities == 0 ? "缺貨" : "低庫存",
            })
            .ToList();

        return new DashboardViewModel
        {
            OrderCount = summary.OrderCount,
            CustomerCount = summary.CustomerCount,
            OpenOrderCount = summary.OpenOrderCount,
            SettledRevenue = summary.SettledRevenue,
            ExpectedRevenue = summary.ExpectedRevenue,
            RecentOrders = recentOrders,
            LowStock = lowStock,
            AuditTotalToday = summary.AuditTotalToday,
            AuditErrorToday = summary.AuditErrorToday,
            LoadFailed = false,
        };
    }
}
