using System.Linq;
using System.Net;
using Shouldly;

namespace SimpleNorthwind.E2E.Tests;

// F4.1：稽核唯讀查詢端點（UserName 解析、method 過濾、summary_date 由新到舊）。
[Collection(E2ECollection.Name)]
public sealed class ApiLogsQueryEndpointTests(CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task QueryApiLogs_AfterAuditedCall_ResolvesUserName_AndIsDescByDate()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        await client.CreateCustomerAsync("ApiLog Probe Co."); // 觸發一筆受稽核寫入

        var root = await (await client.GetAsync("/api/apilogs?page=1&pageSize=15")).ReadJsonAsync();
        root.GetProperty("totalCount").GetInt32().ShouldBeGreaterThan(0);

        var items = root.GetProperty("items");
        items.GetArrayLength().ShouldBeGreaterThan(0);

        // 操作者姓名已由 JOIN employees 解析（acting = 員工 1 Nancy Davolio）
        items.EnumerateArray()
            .Any(i => i.GetProperty("userName").GetString() == AuthHelper.SeedEmployeeName)
            .ShouldBeTrue();

        // summary_date 由新到舊（同格式 yyyy-MM-dd HH:mm:ss 字串可字典序比較）
        string? prev = null;
        foreach (var i in items.EnumerateArray())
        {
            var sd = i.GetProperty("summaryDate").GetString()!;
            if (prev is not null) string.CompareOrdinal(sd, prev).ShouldBeLessThanOrEqualTo(0);
            prev = sd;
        }
    }

    [Fact]
    public async Task QueryApiLogs_MethodFilter_ReturnsOnlyMatchingMethod()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        await client.CreateCustomerAsync("ApiLog Method Co."); // 產生 POST 稽核列

        var root = await (await client.GetAsync("/api/apilogs?method=POST&pageSize=50")).ReadJsonAsync();
        var items = root.GetProperty("items");
        items.GetArrayLength().ShouldBeGreaterThan(0);
        foreach (var i in items.EnumerateArray())
            (i.GetProperty("actionDetail").GetString() ?? "").ShouldStartWith("POST");
    }
}
