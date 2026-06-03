using System.Net.Http.Json;
using Shouldly;

namespace SimpleNorthwind.E2E.Tests;

[Collection(E2ECollection.Name)]
public sealed class ApiLogAuditTests(CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task Login_IsLogged_WithPasswordRedacted()
    {
        await factory.ClearApiLogsAsync();
        var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/auth/login",
            new { employeeId = AuthHelper.SeedEmployeeId, password = AuthHelper.SeedPassword });

        var log = await factory.GetLatestApiLogAsync("Auth.Login");
        log.ShouldNotBeNull();
        log!.Detail.ShouldNotBeNull();
        log.Detail!.ShouldNotContain(AuthHelper.SeedPassword); // 密碼不得入 log
        log.Detail.ShouldContain("***");                       // 已 redact
        log.UserId.ShouldBeNull();                             // 匿名登入無 user
        (DateTime.UtcNow - log.SummaryDate).Duration().ShouldBeLessThan(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public async Task AuthenticatedCall_IsLogged_WithUserId()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        await factory.ClearApiLogsAsync(); // 清掉登入產生的 log，只留下面這次呼叫

        var response = await client.GetAsync("/api/customers");
        response.EnsureSuccessStatusCode();

        var log = await factory.GetLatestApiLogAsync("Customers.List");
        log.ShouldNotBeNull();
        log!.UserId.ShouldBe(AuthHelper.SeedEmployeeId); // 由 JWT sub 解析出的員工編號
    }
}
