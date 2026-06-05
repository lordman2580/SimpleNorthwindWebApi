namespace SimpleNorthwind.WebApi.Web.Models;

/// <summary>
/// 稽核紀錄列呈現模型。<c>Method</c> / <c>Path</c> 由 <c>ActionDetail</c> 解析
/// （格式範例「POST /api/orders | args=...」：第一個 token 為 method、第二個為 path；
/// 若 <c>ActionDetail</c> 為 null 則退回 <c>Actions</c>）。<c>ResponseResultPreview</c>
/// 為回應結果前約 120 字（過長補「…」）；<c>UserName</c> 保留 null（view 顯示「系統 / 匿名」）。
/// </summary>
public sealed class ApiLogViewModel
{
    public Guid Guid { get; init; }
    public string? UserName { get; init; }
    public string Method { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public int? ResponseStatus { get; init; }
    public string? ClientIp { get; init; }
    public int? DurationMs { get; init; }
    public string? ResponseResultPreview { get; init; }
    public string? ResponseResultFull { get; init; }
    public DateTime SummaryDate { get; init; }
}

/// <summary>稽核查詢條件（保留於頁面，供分頁列與過濾表單回填）。日期為使用者本地時間，由 controller 轉 UTC 後送 API。</summary>
public sealed class ApiLogFilterViewModel
{
    public int? UserId { get; init; }
    public string? Method { get; init; }
    public bool OnlyErrors { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
}

/// <summary>稽核列表頁模型：分頁資料 + 目前過濾條件。</summary>
public sealed class ApiLogIndexViewModel
{
    public PagedViewModel<ApiLogViewModel> Page { get; init; } = PagedViewModel<ApiLogViewModel>.From([], 1, 15, 0);
    public ApiLogFilterViewModel Filter { get; init; } = new();
}
