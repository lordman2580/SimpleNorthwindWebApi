namespace SimpleNorthwind.WebApi.Web.Models;

/// <summary>下拉選項：客戶（id 僅作為表單 value，不直接顯示給使用者）。</summary>
public sealed record OrderCustomerOption(int Id, string Name);

/// <summary>下拉選項：產品（id 作為 value，Name 顯示，UnitPrice 供前端試算）。</summary>
public sealed record OrderProductOption(int Id, string Name, decimal UnitPrice);

/// <summary>建立／編輯共用的明細輸入列（model binding 目標，需可變）。</summary>
public sealed class OrderLineInput
{
    /// <summary>產品 id（表單 value）。</summary>
    public int ProductId { get; set; }

    /// <summary>數量。</summary>
    public int OrderQuantities { get; set; }

    /// <summary>折扣百分比（0..100）。</summary>
    public decimal Discount { get; set; }

    /// <summary>樂觀並行版本 token（建立時為 0，編輯時帶回原值）。</summary>
    public int Version { get; set; }
}

/// <summary>建立訂單輸入（POST body model binding 目標）。</summary>
public sealed class CreateOrderInputViewModel
{
    /// <summary>客戶 id。</summary>
    public int CustomerId { get; set; }

    /// <summary>明細列。</summary>
    public List<OrderLineInput> Lines { get; set; } = [];
}

/// <summary>編輯訂單輸入（POST body model binding 目標）。</summary>
public sealed class EditOrderInputViewModel
{
    /// <summary>訂單 id。</summary>
    public int OrderId { get; set; }

    /// <summary>明細列（含 Version）。</summary>
    public List<OrderLineInput> Lines { get; set; } = [];
}

/// <summary>建立訂單表單 ViewModel：輸入 + 客戶／產品下拉資料。</summary>
public sealed class CreateOrderFormViewModel
{
    /// <summary>使用者輸入。</summary>
    public CreateOrderInputViewModel Input { get; init; } = new();

    /// <summary>客戶下拉選項。</summary>
    public IReadOnlyList<OrderCustomerOption> Customers { get; init; } = [];

    /// <summary>產品下拉選項。</summary>
    public IReadOnlyList<OrderProductOption> Products { get; init; } = [];
}

/// <summary>編輯訂單表單 ViewModel：現有明細（含 Version）唯讀產品名 + 可編輯數量／折扣。</summary>
public sealed class EditOrderFormViewModel
{
    /// <summary>訂單 id。</summary>
    public int OrderId { get; init; }

    /// <summary>客戶公司名稱（唯讀顯示）。</summary>
    public string CustomerName { get; init; } = string.Empty;

    /// <summary>現有明細列。</summary>
    public IReadOnlyList<OrderLineViewModel> Lines { get; init; } = [];
}
