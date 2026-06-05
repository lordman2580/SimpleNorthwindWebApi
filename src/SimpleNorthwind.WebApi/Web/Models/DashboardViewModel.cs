namespace SimpleNorthwind.WebApi.Web.Models;

/// <summary>
/// 總覽（Dashboard）頁面檢視模型：KPI 數字、最新訂單、庫存預警與今日稽核摘要。
/// 全部資料由後端 <c>DashboardSummaryDto</c> 映射而來，View 僅負責呈現。
/// </summary>
public sealed class DashboardViewModel
{
    /// <summary>訂單總數。</summary>
    public int OrderCount { get; init; }

    /// <summary>客戶總數。</summary>
    public int CustomerCount { get; init; }

    /// <summary>未結（進行中）訂單數。</summary>
    public int OpenOrderCount { get; init; }

    /// <summary>本期營收（已折扣後）。</summary>
    public decimal Revenue { get; init; }

    /// <summary>最新訂單列表。</summary>
    public IReadOnlyList<DashboardOrderRow> RecentOrders { get; init; } = [];

    /// <summary>庫存預警（缺貨 / 低庫存）列表。</summary>
    public IReadOnlyList<DashboardStockRow> LowStock { get; init; } = [];

    /// <summary>今日稽核總筆數。</summary>
    public int AuditTotalToday { get; init; }

    /// <summary>今日稽核異常筆數。</summary>
    public int AuditErrorToday { get; init; }

    /// <summary>彙總載入是否失敗（true 時 View 顯示空白 / 錯誤提示）。</summary>
    public bool LoadFailed { get; init; }
}

/// <summary>
/// 總覽頁「最新訂單」表格的單列資料。
/// </summary>
public sealed class DashboardOrderRow
{
    /// <summary>訂單代碼。</summary>
    public int OrderId { get; init; }

    /// <summary>客戶名稱（已 enrich，不顯示 FK id）。</summary>
    public string CustomerName { get; init; } = string.Empty;

    /// <summary>承辦員工名稱（已 enrich）。</summary>
    public string EmployeeName { get; init; } = string.Empty;

    /// <summary>訂購日期。</summary>
    public DateTime OrderDate { get; init; }

    /// <summary>訂單狀態字串（Normal / PaidOff / Canceled）。</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>訂單金額（已折扣後合計）。</summary>
    public decimal Total { get; init; }
}

/// <summary>
/// 總覽頁「庫存預警」清單的單列資料。
/// </summary>
public sealed class DashboardStockRow
{
    /// <summary>產品名稱（已 enrich）。</summary>
    public string ProductName { get; init; } = string.Empty;

    /// <summary>分類名稱（已 enrich，不顯示 FK id）。</summary>
    public string CategoryName { get; init; } = string.Empty;

    /// <summary>目前庫存數量。</summary>
    public int Quantities { get; init; }

    /// <summary>庫存狀態（缺貨 / 低庫存）。</summary>
    public string StockStatus { get; init; } = string.Empty;
}
