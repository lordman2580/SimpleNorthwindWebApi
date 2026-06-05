namespace SimpleNorthwind.Application.Common;

/// <summary>分頁查詢結果包裝（清單 + 頁碼 + 每頁筆數 + 總筆數）。供 products / apilogs 等清單端點共用。</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
