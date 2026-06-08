using Microsoft.AspNetCore.Html;

namespace SimpleNorthwind.WebApi.Web.Helpers;

/// <summary>
/// 列表排序表頭的共用邏輯（單一事實）：統一箭頭符號（▲ 升 / ▼ 降）與「點他欄一律升冪」策略，
/// 取代各 list view 不一致的 sortArrow / SortLink（▲▼ vs ▴▾、預設 desc 不一）
/// （見 29-前端共用模組抽取稽核 #6 / §0.2）。
/// </summary>
public static class SortHeaders
{
    /// <summary>目前排序欄顯示 ▲（升）/ ▼（降），其餘欄不顯示。回 <see cref="IHtmlContent"/> 以免被 HTML 編碼。</summary>
    public static IHtmlContent Arrow(string field, string? currentSort, bool desc)
        => string.Equals(field, currentSort, StringComparison.OrdinalIgnoreCase)
            ? new HtmlString(desc ? " ▼" : " ▲")
            : HtmlString.Empty;

    /// <summary>點擊欄位後的排序方向：同欄反轉、他欄一律升冪（desc=false）。</summary>
    public static bool NextDesc(string field, string? currentSort, bool currentDesc)
        => string.Equals(field, currentSort, StringComparison.OrdinalIgnoreCase) && !currentDesc;
}
