using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleNorthwind.WebApi.Filters;

namespace SimpleNorthwind.WebApi.Web.Controllers;

/// <summary>
/// MVC UI controller 共用基底。
/// <para>
/// <b>[Authorize(Cookie)]</b>：全站預設 authentication scheme 是 JWT，若不顯式指定，UI 請求的
/// <c>HttpContext.User</c> 會落到 JWT scheme（cookie 請求無 bearer → 匿名），導致 antiforgery
/// 驗證時 claim-based user 與產生 token 時（Cookie 認證的 user）不符而 400。指定 Cookie scheme 後
/// 所有 UI 動作（含登出）的 User 即為 cookie 身分；登入 / denied 以 <c>[AllowAnonymous]</c> 例外。
/// </para>
/// <para>
/// <see cref="SkipApiLogAttribute"/>（Inherited）使 UI 動作本身不稽核，僅其 loopback <c>/api/*</c>
/// 呼叫留痕，避免雙重稽核。見 19-前端架構與整合 §3 / §8。
/// </para>
/// </summary>
[SkipApiLog]
[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
public abstract class UiControllerBase : Controller
{
    protected const string CookieScheme = CookieAuthenticationDefaults.AuthenticationScheme;

    /// <summary>
    /// loopback 回應若為 401（JWT 過期 / 失效）→ 清 Cookie 並導回登入頁。
    /// 回傳非 <c>null</c> 表示已接管回應，呼叫端應直接 <c>return</c>；回 <c>null</c> 表示非 401，續一般處理。
    /// F2 各資源 controller 共用此 helper。
    /// </summary>
    protected async Task<IActionResult?> RedirectToLoginIfUnauthorizedAsync(HttpStatusCode statusCode)
    {
        if (statusCode != HttpStatusCode.Unauthorized)
            return null;

        await HttpContext.SignOutAsync(CookieScheme).ConfigureAwait(false);
        return LocalRedirect("/account/login?expired=1");
    }
}
