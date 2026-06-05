namespace SimpleNorthwind.WebApi.Web.Models;

/// <summary>訂單明細單列：以 ProductName 呈現給使用者；ProductId 僅作為編輯時 round-trip 的隱藏 token，不直接顯示。折扣為百分比（0..100）。</summary>
public sealed class OrderLineViewModel
{
    /// <summary>產品 id（編輯表單隱藏欄位 round-trip，畫面不顯示原始 id）。</summary>
    public int ProductId { get; init; }

    /// <summary>產品名稱（enrich）。</summary>
    public string ProductName { get; init; } = string.Empty;

    /// <summary>單價。</summary>
    public decimal UnitPrice { get; init; }

    /// <summary>數量。</summary>
    public int Quantity { get; init; }

    /// <summary>折扣百分比（顯示 "{Discount}%"）。</summary>
    public decimal Discount { get; init; }

    /// <summary>樂觀並行版本 token（編輯時帶回）。</summary>
    public int Version { get; init; }

    /// <summary>小計 = UnitPrice × Quantity × (1 - Discount/100)。</summary>
    public decimal LineTotal { get; init; }
}

/// <summary>訂單明細頁 ViewModel：客戶資訊 + 明細列 + 總計。</summary>
public sealed class OrderDetailViewModel
{
    /// <summary>訂單自身編號。</summary>
    public int OrderId { get; init; }

    /// <summary>客戶公司名稱。</summary>
    public string CustomerName { get; init; } = string.Empty;

    /// <summary>客戶自身代碼（可作為標籤識別）。</summary>
    public int CustomerId { get; init; }

    /// <summary>客戶聯絡人（best-effort，可能為 null）。</summary>
    public string? CustomerContactName { get; init; }

    /// <summary>客戶聯絡電話（best-effort，可能為 null）。</summary>
    public string? CustomerContactNumber { get; init; }

    /// <summary>建立員工姓名。</summary>
    public string EmployeeName { get; init; } = string.Empty;

    /// <summary>訂購日（已為瀏覽器本地時間）。</summary>
    public DateTime OrderDate { get; init; }

    /// <summary>狀態原字串：Normal / PaidOff / Canceled。</summary>
    public string Status { get; init; } = "Normal";

    /// <summary>是否已取消。</summary>
    public bool IsCanceled { get; init; }

    /// <summary>是否已付清。</summary>
    public bool IsPaidoff { get; init; }

    /// <summary>明細列。</summary>
    public IReadOnlyList<OrderLineViewModel> Lines { get; init; } = [];

    /// <summary>總計 = 各明細小計加總（無運費）。</summary>
    public decimal GrandTotal { get; init; }
}
