using System.ComponentModel.DataAnnotations;

namespace SimpleNorthwind.WebApi.Web.Models;

/// <summary>
/// 登入表單（雙登入：員工編號 <b>或</b> first + last name，皆 case-sensitive，UD11）。
/// 模式相依欄位於 controller 條件驗證（DataAnnotations 不易表達條件必填）。
/// </summary>
public sealed class LoginViewModel
{
    /// <summary>登入模式："id"（員工編號）｜ "name"（姓名）。預設 name。</summary>
    public string LoginMode { get; set; } = "name";

    [Display(Name = "員工編號")]
    public int? EmployeeId { get; set; }

    [Display(Name = "名 (First name)")]
    public string? FirstName { get; set; }

    [Display(Name = "姓 (Last name)")]
    public string? LastName { get; set; }

    [Required(ErrorMessage = "請輸入密碼。")]
    [DataType(DataType.Password)]
    [Display(Name = "密碼")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "保持登入")]
    public bool RememberMe { get; set; }

    /// <summary>登入成功後導回的本地路徑（防 open-redirect，導向時以 LocalRedirect 驗證）。</summary>
    public string? ReturnUrl { get; set; }

    /// <summary>非欄位層級訊息（帳號或密碼錯誤、權限不足、登入過期…）。</summary>
    public string? ErrorMessage { get; set; }
}
