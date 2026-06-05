namespace SimpleNorthwind.WebApi.Web.Models;

/// <summary>訂單清單單列：FK 一律以名稱呈現（CustomerName / EmployeeName），不出現原始 id。</summary>
public sealed class OrderListItemViewModel
{
    /// <summary>訂單自身編號（可作為標籤識別 訂單 #）。</summary>
    public int OrderId { get; init; }

    /// <summary>客戶公司名稱（enrich）。</summary>
    public string CustomerName { get; init; } = string.Empty;

    /// <summary>建立員工姓名（enrich）。</summary>
    public string EmployeeName { get; init; } = string.Empty;

    /// <summary>訂購日（已為瀏覽器本地時間）。</summary>
    public DateTime OrderDate { get; init; }

    /// <summary>狀態原字串：Normal / PaidOff / Canceled。</summary>
    public string Status { get; init; } = "Normal";

    /// <summary>明細筆數。</summary>
    public int ItemCount { get; init; }

    /// <summary>訂單金額合計（含折扣）。</summary>
    public decimal Total { get; init; }
}

/// <summary>訂單清單頁 ViewModel：已過濾＋排序＋分頁後的資料，加上目前篩選條件與員工下拉選項。</summary>
public sealed class OrderIndexViewModel
{
    /// <summary>已過濾／排序／分頁後的清單。</summary>
    public PagedViewModel<OrderListItemViewModel> Page { get; init; } = PagedViewModel<OrderListItemViewModel>.From([], 1, 10, 0);

    /// <summary>目前狀態頁籤：all / normal / paidoff / canceled。</summary>
    public string StatusTab { get; init; } = "all";

    /// <summary>員工姓名篩選（包含比對）。</summary>
    public string? EmployeeName { get; init; }

    /// <summary>訂購日起（含）。</summary>
    public DateTime? FromDate { get; init; }

    /// <summary>訂購日迄（含）。</summary>
    public DateTime? ToDate { get; init; }

    /// <summary>排序欄位：date / customer / total。</summary>
    public string SortBy { get; init; } = "date";

    /// <summary>是否遞減排序。</summary>
    public bool Desc { get; init; } = true;

    /// <summary>員工姓名去重清單（供篩選下拉）。</summary>
    public IReadOnlyList<string> EmployeeNames { get; init; } = [];
}
