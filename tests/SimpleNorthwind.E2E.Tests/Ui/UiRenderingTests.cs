using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;

namespace SimpleNorthwind.E2E.Tests;

/// <summary>
/// MVC 前端「實際輸出 HTML」驗證（29-前端共用模組抽取稽核 重構後）：以 Cookie 登入後 GET 各頁，
/// 斷言伺服器渲染結果，確認 TagHelper 已綁定（非字面輸出）、partial / 排序 / 分頁 / flash 正確。
/// 補 F4（[[27-Checkpoint-F4-測試與交付]]）UI 煙霧的可自動化部分。
/// </summary>
[Collection(E2ECollection.Name)]
public sealed class UiRenderingTests(CustomWebApplicationFactory factory)
{
    private static readonly Regex TokenRegex =
        new("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"", RegexOptions.Compiled);

    /// <summary>以 UI 流程（GET 登入頁取 antiforgery → POST 員工編號登入）建立帶 auth cookie 的 client。</summary>
    private async Task<HttpClient> LoginUiAsync()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var page = await client.GetAsync("/account/login");
        page.StatusCode.ShouldBe(HttpStatusCode.OK);
        var token = TokenRegex.Match(await page.Content.ReadAsStringAsync()).Groups[1].Value;
        token.ShouldNotBeNullOrEmpty();

        var post = await client.PostAsync("/account/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["LoginMode"] = "id",
            ["EmployeeId"] = AuthHelper.SeedEmployeeId.ToString(),
            ["Password"] = AuthHelper.SeedPassword,
            ["__RequestVerificationToken"] = token,
        }));
        post.StatusCode.ShouldBe(HttpStatusCode.Redirect);   // 302 → "/"
        return client;
    }

    private static async Task<string> GetHtmlAsync(HttpClient client, string path)
    {
        var res = await client.GetAsync(path);
        res.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await res.Content.ReadAsStringAsync();
    }

    [Fact]
    public async Task OrdersList_StatusBadgeTagHelper_AndSortHeaders_AreBound()
    {
        var client = await LoginUiAsync();
        var html = await GetHtmlAsync(client, "/orders");

        // #3 <status-badge>：已綁定（非字面），輸出 rendered span class
        html.ShouldNotContain("<status-badge");
        html.ShouldContain("status-badge badge bg-");

        // #6 排序連結 + 箭頭（控制器預設訂購日 desc → 該欄顯示 ▼）
        html.ShouldContain("sortBy=customer");
        html.ShouldContain("▼");

        // #14 顯示用日期格式
        Regex.IsMatch(html, @"\d{4}-\d{2}-\d{2}").ShouldBeTrue();
    }

    [Fact]
    public async Task OrdersList_EmptyRowTagHelper_IsBound_WhenNoMatch()
    {
        var client = await LoginUiAsync();
        var html = await GetHtmlAsync(client, "/orders?employeeName=__no_such_employee__");

        html.ShouldNotContain("<empty-row");          // #11 已綁定
        html.ShouldContain("沒有符合條件的訂單");
    }

    [Fact]
    public async Task MissingOrder_FailRedirect_SetsFlash_OnIndex()
    {
        var client = await LoginUiAsync();

        var redirect = await client.GetAsync("/orders/999999");
        redirect.StatusCode.ShouldBe(HttpStatusCode.Redirect);          // #2 FailRedirectAsync → Index
        redirect.Headers.Location!.OriginalString.ShouldBe("/orders");

        var html = await GetHtmlAsync(client, "/orders");
        html.ShouldContain("alert alert-danger");                       // #1 flash partial（_Layout 統一渲染）
        html.ShouldContain("找不到訂單 #999999");                        // #8 ResolveErrorMessage(notFound)
    }

    [Fact]
    public async Task SortLinks_PreserveActiveFilter()
    {
        var client = await LoginUiAsync();
        var html = await GetHtmlAsync(client, "/orders?statusTab=normal");

        html.ShouldContain("statusTab=normal");
        // #5 QueryString.Build：排序連結保留 statusTab（BuildQuery 參數順序 statusTab 先於 sortBy）
        Regex.IsMatch(html, @"/orders\?[^""]*statusTab=normal[^""]*sortBy=customer").ShouldBeTrue();
    }

    [Fact]
    public async Task CustomerCreateForm_RendersSharedFieldsPartial()
    {
        var client = await LoginUiAsync();
        var html = await GetHtmlAsync(client, "/customers/create");

        // #9 _CustomerFormFields：欄位名無前綴（代表 partial 以同一 model 綁定，送出可正確 model-bind）
        html.ShouldContain("name=\"CompanyName\"");
        html.ShouldContain("name=\"ContactName\"");
        html.ShouldContain("name=\"Email\"");
    }

    [Fact]
    public async Task ApiLogsPage_WiresSignalRLiveClient_AndServesStaticAssets()
    {
        var client = await LoginUiAsync();
        var html = await GetHtmlAsync(client, "/apilogs");

        // F3.3 即時推播用戶端串接
        html.ShouldContain("id=\"logRows\"");                 // prepend 目標 tbody
        html.ShouldContain("/lib/signalr/signalr.min.js");    // SignalR 用戶端
        html.ShouldContain("/js/apilog-live.js");             // 即時腳本
        html.ShouldContain("id=\"logFilter\"");               // 過濾資料島
        html.ShouldContain("\"onlyErrors\"");                 // 過濾 JSON 有效

        // 靜態資產可服務（content root = WebApi/wwwroot）
        (await client.GetAsync("/lib/signalr/signalr.min.js")).StatusCode.ShouldBe(HttpStatusCode.OK);
        var js = await client.GetAsync("/js/apilog-live.js");
        js.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await js.Content.ReadAsStringAsync()).ShouldContain("/hubs/apilogs");
    }

    [Fact]
    public async Task Pagination_Renders_AndPreservesFilter()
    {
        // 確保 >1 頁（種子客戶 + 12 筆）
        var apiClient = await factory.CreateAuthenticatedClientAsync();
        for (var i = 0; i < 12; i++)
            await apiClient.CreateCustomerAsync($"PageCo {i:D2}");

        var client = await LoginUiAsync();
        var html = await GetHtmlAsync(client, "/customers?pageSize=10");

        html.ShouldNotContain("<partial");        // #4 partial 已處理（非字面）
        html.ShouldContain("pagination");          // _Pagination 導覽列
        html.ShouldContain("page=2");              // 下一頁連結（保留 pageSize/排序）
    }
}
