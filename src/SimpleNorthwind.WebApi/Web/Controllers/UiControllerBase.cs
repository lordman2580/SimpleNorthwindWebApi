using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleNorthwind.Infrastructure.Time;
using SimpleNorthwind.WebApi.Filters;
using SimpleNorthwind.WebApi.Web.Http;

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

    private const string TimeZoneCookie = "tz";

    /// <summary>
    /// 將使用者本地時間（datetime-local query 參數，<c>Kind=Unspecified</c>）轉為 UTC 牆鐘值並去除 Kind。
    /// <para>
    /// 用於 loopback API 的日期區間參數（如稽核 from/to）：DB <c>summary_date</c> 存 UTC，
    /// SQL 直接比對 <c>@fromUtc</c>/<c>@toUtc</c>，故 UI 端須先把瀏覽器本地時間轉成 UTC。
    /// 回傳 <c>Kind=Unspecified</c>（不帶 Z）以免 <c>NorthwindApiClient</c> 的 <c>"o"</c> 序列化帶 Z 後，
    /// 被 API 端 query-string <see cref="DateTime"/> 綁定再做一次伺服器時區位移。
    /// 時區取自瀏覽器 <c>tz</c> cookie（IANA id），缺漏 / 無法解析退回
    /// <see cref="ClientTimeZoneAccessor.Default"/>（App:DefaultTimeZone）。
    /// </para>
    /// </summary>
    protected DateTime? ToClientUtc(DateTime? clientLocal)
    {
        if (clientLocal is not { } local)
            return null;

        var utc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(local, DateTimeKind.Unspecified), ResolveClientTimeZone());
        return DateTime.SpecifyKind(utc, DateTimeKind.Unspecified);
    }

    /// <summary>由 <c>tz</c> cookie 解析瀏覽器時區（與 <c>BearerTimeZoneHandler</c> 同源），缺漏退回預設時區。</summary>
    private TimeZoneInfo ResolveClientTimeZone()
    {
        var id = Request.Cookies[TimeZoneCookie];
        if (!string.IsNullOrWhiteSpace(id))
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        return ClientTimeZoneAccessor.Default;
    }

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

    /// <summary>
    /// loopback 失敗的統一導向：先處理 401（→ 登入），否則將狀態碼對映之錯誤訊息寫入
    /// <c>TempData["Error"]</c> 並 <see cref="ControllerBase.RedirectToAction(string)"/> 至指定 action。
    /// 僅在 <c>!result.IsSuccess</c> 時呼叫（401 亦屬失敗，於此被攔下）。
    /// 取代 F2 各 controller 重複的「401 檢查 + 狀態 switch + TempData + RedirectToAction」樣板
    /// （見 29-前端共用模組抽取稽核 #2 / #8）。
    /// </summary>
    protected async Task<IActionResult> FailRedirectAsync<T>(
        ApiResult<T> result,
        string action,
        object? routeValues = null,
        string fallback = "操作失敗。",
        string? notFound = null,
        string? conflict = null,
        string? badRequest = null)
    {
        if (await RedirectToLoginIfUnauthorizedAsync(result.StatusCode).ConfigureAwait(false) is { } login)
            return login;

        TempData["Error"] = ResolveErrorMessage(result, fallback, notFound, conflict, badRequest);
        return routeValues is null
            ? RedirectToAction(action)
            : RedirectToAction(action, routeValues);
    }

    /// <summary>
    /// 由失敗 <see cref="ApiResult{T}"/> 對映人類可讀錯誤訊息。語意刻意對齊 F2 既有行為：
    /// NotFound → 固定友善訊息（忽略 API Detail，呈現可帶 id 的中文）；Conflict / BadRequest → 優先 API
    /// <c>Detail</c>、缺漏才退回固定訊息；其餘 → <c>Detail ?? fallback</c>。供 View-on-failure 站點亦可重用。
    /// </summary>
    protected static string ResolveErrorMessage<T>(
        ApiResult<T> result,
        string fallback,
        string? notFound = null,
        string? conflict = null,
        string? badRequest = null)
        => result.StatusCode switch
        {
            HttpStatusCode.NotFound when notFound is not null => notFound,
            HttpStatusCode.Conflict when conflict is not null => result.Detail ?? conflict,
            HttpStatusCode.BadRequest when badRequest is not null => result.Detail ?? badRequest,
            _ => result.Detail ?? fallback,
        };
}
