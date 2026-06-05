namespace SimpleNorthwind.WebApi.Web.Models;

/// <summary>
/// 清單分頁呈現包裝：對映 API 的 <c>PagedResult&lt;T&gt;</c>，加上頁碼計算供 view 產生分頁列。
/// </summary>
public sealed class PagedViewModel<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public int TotalCount { get; init; }

    public int TotalPages => PageSize <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
    public int FirstItemIndex => TotalCount == 0 ? 0 : ((Page - 1) * PageSize) + 1;
    public int LastItemIndex => Math.Min(Page * PageSize, TotalCount);

    /// <summary>每頁筆數選項（產品 / 稽核共用）。</summary>
    public static int[] PageSizeOptions => [10, 30, 50, 100];

    public static PagedViewModel<T> From(IReadOnlyList<T> items, int page, int pageSize, int totalCount)
        => new() { Items = items, Page = page, PageSize = pageSize, TotalCount = totalCount };
}
