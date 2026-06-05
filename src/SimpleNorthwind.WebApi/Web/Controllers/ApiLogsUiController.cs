using Microsoft.AspNetCore.Mvc;
using SimpleNorthwind.Application.ApiLogs;
using SimpleNorthwind.WebApi.Web.Http;
using SimpleNorthwind.WebApi.Web.Models;

namespace SimpleNorthwind.WebApi.Web.Controllers;

/// <summary>API 稽核唯讀列表 UI controller（伺服器分頁 15/頁；方法 / 路徑由 ActionDetail 解析後呈現）。</summary>
[Route("apilogs")]
public sealed class ApiLogsUiController(NorthwindApiClient apiClient) : UiControllerBase
{
    private const int PreviewLength = 120;

    /// <summary>稽核列表：依過濾條件 + 分頁查詢，並把條件回填於 view 供表單 / 分頁列重用。</summary>
    [HttpGet("")]
    public async Task<IActionResult> Index(
        int? userId,
        string? method,
        bool onlyErrors = false,
        DateTime? from = null,
        DateTime? to = null,
        int page = 1,
        int pageSize = 15,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 15;

        var filter = new ApiLogFilterViewModel
        {
            UserId = userId,
            Method = string.IsNullOrWhiteSpace(method) ? null : method,
            OnlyErrors = onlyErrors,
            From = from,
            To = to
        };

        var result = await apiClient.ListApiLogsAsync(userId, filter.Method, onlyErrors, from, to, page, pageSize, ct);
        if (await RedirectToLoginIfUnauthorizedAsync(result.StatusCode) is { } redirect) return redirect;

        if (!result.IsSuccess || result.Value is null)
        {
            TempData["Error"] = result.Detail ?? "稽核紀錄載入失敗。";
            return View("~/Web/Views/ApiLogs/Index.cshtml", new ApiLogIndexViewModel { Filter = filter });
        }

        var paged = result.Value;
        var items = paged.Items.Select(MapToViewModel).ToList();
        var vm = new ApiLogIndexViewModel
        {
            Page = PagedViewModel<ApiLogViewModel>.From(items, paged.Page, paged.PageSize, paged.TotalCount),
            Filter = filter
        };
        return View("~/Web/Views/ApiLogs/Index.cshtml", vm);
    }

    /// <summary>DTO → 列模型：解析 method / path，截斷回應結果預覽。</summary>
    private static ApiLogViewModel MapToViewModel(ApiLogDto dto)
    {
        var (method, path) = ParseMethodAndPath(dto.ActionDetail, dto.Actions);
        return new ApiLogViewModel
        {
            Guid = dto.Guid,
            UserName = dto.UserName,
            Method = method,
            Path = path,
            ResponseStatus = dto.ResponseStatus,
            ClientIp = dto.ClientIp,
            DurationMs = dto.DurationMs,
            ResponseResultPreview = BuildPreview(dto.ResponseResult),
            ResponseResultFull = dto.ResponseResult,
            SummaryDate = dto.SummaryDate
        };
    }

    /// <summary>
    /// 由 ActionDetail（如「POST /api/orders | args=...」）取 method（第一 token）與 path（第二 token）；
    /// ActionDetail 為 null/空白時退回 Actions 作為 method、path 留空。
    /// </summary>
    private static (string Method, string Path) ParseMethodAndPath(string? actionDetail, string actions)
    {
        if (string.IsNullOrWhiteSpace(actionDetail))
            return (actions, string.Empty);

        var tokens = actionDetail.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var method = tokens.Length > 0 ? tokens[0] : actions;
        var path = tokens.Length > 1 ? tokens[1] : string.Empty;
        return (method, path);
    }

    /// <summary>回應結果預覽：取前約 120 字，過長補省略號。</summary>
    private static string? BuildPreview(string? responseResult)
    {
        if (string.IsNullOrEmpty(responseResult))
            return responseResult;

        return responseResult.Length <= PreviewLength
            ? responseResult
            : string.Concat(responseResult.AsSpan(0, PreviewLength), "…");
    }
}
