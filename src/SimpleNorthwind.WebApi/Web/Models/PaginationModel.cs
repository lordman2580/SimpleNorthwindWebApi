namespace SimpleNorthwind.WebApi.Web.Models;

/// <summary>
/// 分頁導覽列（上一頁 / 第 X / Y 頁 / 下一頁）的共用模型，供 <c>_Pagination</c> partial 使用。
/// 每頁筆數選擇器仍由各 view 自行渲染（版面各異）。見 29-前端共用模組抽取稽核 #4。
/// </summary>
public sealed class PaginationModel
{
    public required int Page { get; init; }
    public required int TotalPages { get; init; }
    public required bool HasPrevious { get; init; }
    public required bool HasNext { get; init; }

    /// <summary>頁碼 → 連結（由 view 以 <c>QueryString.Build</c> 提供，保留目前過濾條件）。</summary>
    public required Func<int, string> PageHref { get; init; }
}
