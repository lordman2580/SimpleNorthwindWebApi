using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using SimpleNorthwind.Infrastructure.Options;

namespace SimpleNorthwind.WebApi.Web.Http;

/// <summary>
/// loopback 請求前置處理（見 19-前端架構與整合 §4.3）：
/// 1) 從目前使用者的 Cookie auth ticket 取 <c>jwt</c> claim → 補 <c>Authorization: Bearer</c>。
/// 2) 從 <c>tz</c> cookie 取瀏覽器 IANA 時區 → 補 <c>X-Time-Zone</c>；缺漏退回 <c>App:DefaultTimeZone</c>。
/// 既有 <c>ClientTimeZoneMiddleware</c> 讀此 header，API 輸出即為瀏覽器本地時區。
/// </summary>
public sealed class BearerTimeZoneHandler(
    IHttpContextAccessor httpContextAccessor,
    IOptions<AppOptions> appOptions) : DelegatingHandler
{
    private const string JwtClaimType = "jwt";
    private const string TimeZoneCookie = "tz";
    private const string TimeZoneHeader = "X-Time-Zone";

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var context = httpContextAccessor.HttpContext;

        var jwt = context?.User.FindFirst(JwtClaimType)?.Value;
        if (!string.IsNullOrEmpty(jwt))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        var timeZone = context?.Request.Cookies[TimeZoneCookie];
        if (string.IsNullOrWhiteSpace(timeZone))
            timeZone = appOptions.Value.DefaultTimeZone;

        request.Headers.Remove(TimeZoneHeader);
        request.Headers.Add(TimeZoneHeader, timeZone);

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
