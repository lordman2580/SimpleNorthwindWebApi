namespace SimpleNorthwind.WebApi.Web.Models;

/// <summary>
/// 客戶詳情頁檢視模型：基本資料、外訪狀態、客戶概況統計與訂單紀錄。
/// </summary>
public sealed class CustomerDetailViewModel
{
    /// <summary>客戶代碼（自身識別，可顯示）。</summary>
    public int CustomerId { get; init; }

    /// <summary>公司名稱。</summary>
    public string CompanyName { get; init; } = string.Empty;

    /// <summary>聯絡人姓名。</summary>
    public string? ContactName { get; init; }

    /// <summary>聯絡人職稱。</summary>
    public string? ContactTitle { get; init; }

    /// <summary>聯絡電話。</summary>
    public string? ContactNumber { get; init; }

    /// <summary>Email。</summary>
    public string? Email { get; init; }

    /// <summary>建立日期。</summary>
    public DateTime CreateDate { get; init; }

    /// <summary>建立者。</summary>
    public string CreateUser { get; init; } = string.Empty;

    /// <summary>是否已外訪。</summary>
    public bool IsOutContacted { get; init; }

    /// <summary>外訪日期。</summary>
    public DateTime? OutContactedDate { get; init; }

    /// <summary>客戶的訂單紀錄列表。</summary>
    public IReadOnlyList<CustomerOrderRow> Orders { get; init; } = [];

    /// <summary>累計訂單數。</summary>
    public int TotalOrders { get; init; }

    /// <summary>累計消費金額。</summary>
    public decimal TotalSpent { get; init; }

    /// <summary>最近一筆訂單日期。</summary>
    public DateTime? LastOrderDate { get; init; }
}

/// <summary>
/// 客戶詳情頁的訂單紀錄列：訂單編號、訂購日、狀態與金額（明細加總）。
/// </summary>
public sealed class CustomerOrderRow
{
    /// <summary>訂單編號。</summary>
    public int OrderId { get; init; }

    /// <summary>訂購日期。</summary>
    public DateTime OrderDate { get; init; }

    /// <summary>狀態字串（"Normal"｜"PaidOff"｜"Canceled"）。</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>訂單金額（明細小計加總）。</summary>
    public decimal Total { get; init; }
}
