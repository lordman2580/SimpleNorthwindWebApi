namespace SimpleNorthwind.WebApi.Web.Models;

/// <summary>
/// 員工名冊（唯讀）卡片格的顯示模型。
/// 僅承載可公開呈現的欄位，絕不含密碼。
/// </summary>
public sealed class EmployeeViewModel
{
    /// <summary>員工代碼（實體自身識別碼，可顯示）。</summary>
    public int EmployeeId { get; init; }

    /// <summary>員工姓名（後端已組合的全名）。</summary>
    public required string FullName { get; init; }

    /// <summary>職稱。</summary>
    public string? Title { get; init; }

    /// <summary>聯絡電話。</summary>
    public string? PhoneNumber { get; init; }

    /// <summary>電話分機。</summary>
    public string? PhoneExtNumber { get; init; }

    /// <summary>到職日期。</summary>
    public DateTime? HireDate { get; init; }

    /// <summary>是否已離職。</summary>
    public bool IsResigned { get; init; }

    /// <summary>在職狀態文字（離職顯示「已離職」，否則「在職」）。</summary>
    public string StatusText => IsResigned ? "已離職" : "在職";
}
