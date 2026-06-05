using System.Net;
using System.Text.Json;
using Shouldly;

namespace SimpleNorthwind.E2E.Tests;

// F4.1：員工唯讀端點 —— 回應含姓名、且【絕不含密碼欄位】（安全驗收）。
[Collection(E2ECollection.Name)]
public sealed class EmployeesEndpointTests(CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task ListEmployees_ReturnsNames_AndNeverExposesPassword()
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        var res = await client.GetAsync("/api/employees");
        res.StatusCode.ShouldBe(HttpStatusCode.OK);

        // 原始 JSON 不得出現任何 password 痕跡
        var raw = (await res.Content.ReadAsStringAsync()).ToLowerInvariant();
        raw.ShouldNotContain("password");
        raw.ShouldNotContain("passwordhash");

        var root = JsonDocument.Parse(raw).RootElement;
        root.GetArrayLength().ShouldBeGreaterThan(0);                 // 回傳的是陣列（非分頁）
        root[0].GetProperty("fullname").GetString().ShouldNotBeNullOrEmpty();
    }
}
