using System.ComponentModel.DataAnnotations;

namespace SimpleNorthwind.WebApi.Web.Models;

/// <summary>
/// 客戶清單列檢視模型：對映 <c>CustomerDto</c> 的呈現欄位，外加由訂單群組計得的 <see cref="OrderCount"/>。
/// </summary>
public sealed class CustomerViewModel
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

    /// <summary>該客戶的訂單數（由 <c>ListOrdersAsync</c> 依 CustomerId 群組計得；取數失敗則為 0）。</summary>
    public int OrderCount { get; init; }
}

/// <summary>
/// 客戶清單頁檢視模型：包裝分頁資料與目前排序狀態（排序 / 分頁皆於 controller 記憶體處理）。
/// </summary>
public sealed class CustomerIndexViewModel
{
    /// <summary>分頁後的客戶列表。</summary>
    public PagedViewModel<CustomerViewModel> Page { get; init; } = PagedViewModel<CustomerViewModel>.From([], 1, 10, 0);

    /// <summary>排序欄位："company"｜"contact"｜"orders"。</summary>
    public string SortBy { get; init; } = "company";

    /// <summary>是否遞減排序。</summary>
    public bool Desc { get; init; }
}

/// <summary>
/// 客戶新增 / 編輯表單繫結模型（新增時 <see cref="CustomerId"/> 為 <c>null</c>）。
/// </summary>
public sealed class CustomerFormViewModel
{
    /// <summary>客戶代碼（編輯時帶值；新增時為 <c>null</c>）。</summary>
    public int? CustomerId { get; set; }

    /// <summary>公司名稱（必填）。</summary>
    [Required(ErrorMessage = "請輸入公司名稱。")]
    [Display(Name = "公司名稱")]
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>聯絡人姓名。</summary>
    [Display(Name = "聯絡人")]
    public string? ContactName { get; set; }

    /// <summary>聯絡人職稱。</summary>
    [Display(Name = "職稱")]
    public string? ContactTitle { get; set; }

    /// <summary>聯絡電話。</summary>
    [Display(Name = "電話")]
    public string? ContactNumber { get; set; }

    /// <summary>Email。</summary>
    [Display(Name = "Email")]
    public string? Email { get; set; }

    /// <summary>是否已外訪。</summary>
    [Display(Name = "是否外訪")]
    public bool IsOutContacted { get; set; }

    /// <summary>外訪日期。</summary>
    [Display(Name = "外訪日期")]
    [DataType(DataType.Date)]
    public DateTime? OutContactedDate { get; set; }
}
