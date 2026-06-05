using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SimpleNorthwind.WebApi.Web.RealTime;

/// <summary>
/// 稽核即時推播 Hub（單向 server → client）。client 僅訂閱 <c>"apilog"</c> 事件、不呼叫 server 方法，
/// 故無公開 Hub 方法。
/// <para>
/// 掛 <b>Cookie</b> scheme：瀏覽器同源 WebSocket 自動帶 Cookie，沿用 UI 既有 Cookie 驗證，未登入無法連線；
/// 不需 JWT-in-querystring（那是跨源 / Bearer-only 才需要）。與 19-前端架構與整合 §3 雙 scheme 一致：
/// UI / Hub = Cookie，<c>/api/*</c> = JWT。
/// </para>
/// </summary>
[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
public sealed class ApiLogHub : Hub;
