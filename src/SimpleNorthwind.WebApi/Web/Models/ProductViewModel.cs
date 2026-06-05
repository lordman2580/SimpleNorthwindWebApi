namespace SimpleNorthwind.WebApi.Web.Models;

/// <summary>
/// 產品清單單列檢視模型（唯讀）。對映 API 的 <c>ProductDto</c>，
/// 庫存狀態 <see cref="StockStatus"/> 由 <see cref="Quantities"/> 是否為 0 推導，供 view 直接呈現 badge。
/// </summary>
public sealed class ProductViewModel
{
    /// <summary>產品自身識別碼（可作為列標籤顯示，非外鍵）。</summary>
    public int ProductId { get; init; }

    /// <summary>產品名稱。</summary>
    public string ProductName { get; init; } = string.Empty;

    /// <summary>分類名稱（由 API JOIN 取得，不外露 category_id）。</summary>
    public string CategoryName { get; init; } = string.Empty;

    /// <summary>庫存數量。</summary>
    public int Quantities { get; init; }

    /// <summary>單價。</summary>
    public decimal UnitPrice { get; init; }

    /// <summary>庫存狀態文字：庫存為 0 顯示「缺貨」，否則「正常」。</summary>
    public string StockStatus { get; init; } = "正常";
}

/// <summary>
/// 產品清單頁面檢視模型：分頁資料 + 目前篩選 / 排序狀態 + 可用分類清單（供分類 chip 列）。
/// </summary>
public sealed class ProductIndexViewModel
{
    /// <summary>分頁後的產品清單。</summary>
    public PagedViewModel<ProductViewModel> Page { get; init; } = PagedViewModel<ProductViewModel>.From([], 1, 10, 0);

    /// <summary>目前篩選的分類名稱（<c>null</c> 表示全部）。</summary>
    public string? Category { get; init; }

    /// <summary>目前排序欄位：name / category / price / stock。</summary>
    public string SortBy { get; init; } = "name";

    /// <summary>是否降冪排序。</summary>
    public bool Desc { get; init; }

    /// <summary>可篩選的分類名稱清單（供分類 chip 列；best-effort，失敗時為空）。</summary>
    public IReadOnlyList<string> Categories { get; init; } = [];
}
