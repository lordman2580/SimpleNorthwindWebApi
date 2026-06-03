using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleNorthwind.Application.Abstractions.Services;
using SimpleNorthwind.Application.ApiLogs;
using SimpleNorthwind.Application.Common;
using SimpleNorthwind.WebApi.Filters;

namespace SimpleNorthwind.WebApi.Controllers;

/// <summary>
/// API 稽核紀錄唯讀檢視（分頁 + 過濾）。需 JWT。本 controller 標 <c>[SkipApiLog]</c>：
/// 讀稽核**不自我留痕、不觸發推播**（UD8）。
/// </summary>
[ApiController]
[Route("api/apilogs")]
[Authorize]
[SkipApiLog]
public sealed class ApiLogsController(IApiLogQueryService apiLogQueryService) : ControllerBase
{
    /// <summary>分頁查詢稽核紀錄（操作者姓名解析、method 過濾、僅異常、日期區間；summary_date 由新到舊）。</summary>
    /// <param name="userId">過濾操作者（員工編號，可選）。</param>
    /// <param name="method">過濾 HTTP 方法（GET / POST / PUT / DELETE，可選）。</param>
    /// <param name="onlyErrors">僅顯示異常（狀態碼 ≥ 400）。</param>
    /// <param name="from">起始時間（UTC，可選）。</param>
    /// <param name="to">結束時間（UTC，可選）。</param>
    /// <param name="page">頁碼（從 1 起，預設 1）。</param>
    /// <param name="pageSize">每頁筆數（1–100，預設 15，配合捲動載入）。</param>
    /// <param name="ct">取消權杖。</param>
    /// <response code="200">回傳分頁稽核清單（含 totalCount）。</response>
    /// <response code="401">未提供或無效的 JWT。</response>
    [HttpGet]
    public async Task<ActionResult<PagedResult<ApiLogDto>>> List(
        [FromQuery] int? userId = null,
        [FromQuery] string? method = null,
        [FromQuery] bool onlyErrors = false,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 15,
        CancellationToken ct = default)
    {
        var query = new ApiLogQuery(userId, method, onlyErrors, from, to, page, pageSize);
        return Ok(await apiLogQueryService.QueryAsync(query, ct));
    }
}
