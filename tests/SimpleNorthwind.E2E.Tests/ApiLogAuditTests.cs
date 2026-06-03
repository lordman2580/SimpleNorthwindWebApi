using System.Net.Http.Json;
using Shouldly;

namespace SimpleNorthwind.E2E.Tests;

[Collection(E2ECollection.Name)]
public sealed class ApiLogAuditTests(CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task Login_IsLogged_WithPasswordRedacted_AndResponseCaptured()
    {
        await factory.ClearApiLogsAsync();
        var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/auth/login",
            new { employeeId = AuthHelper.SeedEmployeeId, password = AuthHelper.SeedPassword });

        var log = await factory.GetLatestApiLogAsync("Auth.Login");
        log.ShouldNotBeNull();
        log!.Detail.ShouldNotBeNull();
        log.Detail!.ShouldNotContain(AuthHelper.SeedPassword); // 密碼不得入 log
        log.Detail.ShouldContain("***");                       // request 已 redact
        log.UserId.ShouldBeNull();                             // 匿名登入無 user

        // 回應稽核：200 + 回應內容（token 已 redact，不得外洩 JWT）
        log.ResponseStatus.ShouldBe(200);
        log.ResponseResult.ShouldNotBeNull();
        log.ResponseResult!.ShouldContain("***");              // token 已 redact
        log.ResponseResult.ShouldContain("ExpiresAt");         // 仍保留非敏感欄位

        (DateTime.UtcNow - log.SummaryDate).Duration().ShouldBeLessThan(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public async Task AuthenticatedCall_IsLogged_WithUserId_AndStatus200()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        await factory.ClearApiLogsAsync(); // 清掉登入產生的 log，只留下面這次呼叫

        var response = await client.GetAsync("/api/customers");
        response.EnsureSuccessStatusCode();

        var log = await factory.GetLatestApiLogAsync("Customers.List");
        log.ShouldNotBeNull();
        log!.UserId.ShouldBe(AuthHelper.SeedEmployeeId); // 由 JWT sub 解析出的員工編號
        log.ResponseStatus.ShouldBe(200);
    }

    [Fact]
    public async Task NotModifiedUpdate_IsLogged_WithStatus400AndProblemDetails()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var create = await client.PostAsJsonAsync("/api/customers",
            new { companyName = "Audit NoOp Co.", contactNumber = "02-4444-4444", contactTitle = "Owner" });
        var id = (await create.ReadJsonAsync()).GetProperty("customerId").GetInt32();
        await factory.ClearApiLogsAsync();

        // 未修改 → 400 ProblemDetails
        await client.PutAsJsonAsync($"/api/customers/{id}", new
        {
            companyName = "Audit NoOp Co.",
            contactNumber = "02-4444-4444",
            contactTitle = "Owner",
            isOutContacted = false,
            outContactedDate = (string?)null,
        });

        var log = await factory.GetLatestApiLogAsync("Customers.Update");
        log.ShouldNotBeNull();
        log!.ResponseStatus.ShouldBe(400);                          // 稽核到 400
        log.ResponseResult.ShouldNotBeNull();
        log.ResponseResult!.ShouldContain("customer.not_modified"); // ProblemDetails title 入稽核
    }
}
