using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;

namespace SimpleNorthwind.E2E.Tests;

// F4.3：MVC 驗證分流（雙 scheme）+ loopback 取資料渲染名稱。
[Collection(E2ECollection.Name)]
public sealed class UiAuthFlowTests(CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task UnauthenticatedUi_ProtectedPage_RedirectsToLogin()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var res = await client.GetAsync("/orders");
        res.StatusCode.ShouldBe(HttpStatusCode.Redirect);                    // Cookie scheme → 導頁
        res.Headers.Location!.OriginalString.ShouldContain("/account/login");
    }

    [Fact]
    public async Task UnauthenticatedApi_Returns401_NotRedirect()
    {
        var client = factory.CreateClient();

        var res = await client.GetAsync("/api/orders");
        res.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);               // JWT scheme → 401 JSON
    }

    [Fact]
    public async Task LoggedInUi_Loopback_RendersEmployeeNames_NotEmptyState()
    {
        var client = await factory.LoginUiAsync();

        var html = await (await client.GetAsync("/employees")).Content.ReadAsStringAsync();

        html.ShouldContain("員工名冊");
        html.ShouldNotContain("目前沒有員工資料");        // loopback Bearer 成功取得資料（非 fallback 空狀態）
        html.ShouldContain(AuthHelper.SeedEmployeeName);  // 顯示姓名（Nancy Davolio），非員工 id
    }
}
