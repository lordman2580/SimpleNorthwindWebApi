using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Shouldly;

namespace SimpleNorthwind.E2E.Tests;

// F4.4：即時稽核推播（SignalR）整合測試 —— 驗證 F3.1/F3.2/F3.3(server) 鏈路。
//  - 未登入連 Hub → 遭拒（Cookie 驗證）。
//  - 登入後連 Hub，觸發受稽核 API → 收到 apilog 事件，UserName 為姓名、無敏感欄位。
// HubConnection 經 TestServer in-memory handler（LongPolling），Cookie 與 UI 登入共享。
[Collection(E2ECollection.Name)]
public sealed class ApiLogHubTests(CustomWebApplicationFactory factory)
{
    private static readonly Regex AntiforgeryRegex =
        new("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"", RegexOptions.Compiled);

    [Fact]
    public async Task UnauthenticatedConnection_ToHub_IsRejected()
    {
        await using var conn = BuildHubConnection(new CookieContainer()); // 無 auth cookie

        await Should.ThrowAsync<Exception>(async () => await conn.StartAsync());
        conn.State.ShouldBe(HubConnectionState.Disconnected);
    }

    [Fact]
    public async Task AuditedApiCall_BroadcastsApiLogEvent_WithResolvedUserName()
    {
        // 1) 以共享 CookieContainer 完成 UI 登入（取得 auth cookie）
        var cookies = new CookieContainer();
        using (var http = new HttpClient(new CookieHandler(factory.Server.CreateHandler(), cookies))
        {
            BaseAddress = factory.Server.BaseAddress,
        })
        {
            var page = await http.GetAsync("/account/login");
            var token = AntiforgeryRegex.Match(await page.Content.ReadAsStringAsync()).Groups[1].Value;
            var post = await http.PostAsync("/account/login", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["LoginMode"] = "id",
                ["EmployeeId"] = AuthHelper.SeedEmployeeId.ToString(),
                ["Password"] = AuthHelper.SeedPassword,
                ["__RequestVerificationToken"] = token,
            }));
            post.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        }

        // 2) 帶 cookie 連 Hub，訂閱 apilog
        await using var conn = BuildHubConnection(cookies);
        var received = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        conn.On<JsonElement>("apilog", dto =>
        {
            // 只認我們稍後觸發的那筆（POST /api/customers）
            var detail = dto.TryGetProperty("actionDetail", out var d) ? d.GetString() ?? "" : "";
            if (detail.StartsWith("POST") && detail.Contains("/api/customers"))
                received.TrySetResult(dto);
        });
        await conn.StartAsync();
        conn.State.ShouldBe(HubConnectionState.Connected);

        // 3) 另一個 JWT client 觸發受稽核寫入（員工 1）
        var apiClient = await factory.CreateAuthenticatedClientAsync();
        await apiClient.CreateCustomerAsync("Hub Broadcast Co.");

        // 4) 應收到推播
        var completed = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(10)));
        completed.ShouldBe(received.Task, "未在時限內收到 apilog 推播");

        var log = await received.Task;
        log.GetProperty("userName").GetString().ShouldBe(AuthHelper.SeedEmployeeName); // 姓名（免 DB lookup）
        log.GetProperty("responseStatus").GetInt32().ShouldBe((int)HttpStatusCode.Created);
        // 無敏感欄位痕跡
        log.GetRawText().ToLowerInvariant().ShouldNotContain("password");
    }

    private HubConnection BuildHubConnection(CookieContainer cookies)
        => new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress, "hubs/apilogs"), options =>
            {
                // 設了 HttpMessageHandlerFactory 後 options.Cookies 不生效（SignalR 用此 handler、不套 CookieContainer）；
                // 故把 TestServer handler 包進 CookieHandler，讓 negotiate / long-poll 都帶 auth cookie。
                options.HttpMessageHandlerFactory = _ => new CookieHandler(factory.Server.CreateHandler(), cookies);
                options.Transports = HttpTransportType.LongPolling; // WebSocket 於 TestServer 需另接 factory
            })
            .Build();

    // 在 TestServer handler 之上維護 CookieContainer（login 與 Hub 共享同一 jar）。
    private sealed class CookieHandler(HttpMessageHandler inner, CookieContainer cookies) : DelegatingHandler(inner)
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var uri = request.RequestUri!;
            var header = cookies.GetCookieHeader(uri);
            if (!string.IsNullOrEmpty(header)) request.Headers.Add("Cookie", header);

            var response = await base.SendAsync(request, ct).ConfigureAwait(false);
            if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
                foreach (var sc in setCookies) cookies.SetCookies(uri, sc);
            return response;
        }
    }
}
