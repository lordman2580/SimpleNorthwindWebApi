using System.Net;
using System.Net.Http.Json;
using Shouldly;

namespace SimpleNorthwind.E2E.Tests;

// F4.2：既有 E2E 更新驗證 —— OrderDto 的 enrich 欄位（名稱非 id）。
[Collection(E2ECollection.Name)]
public sealed class OrderEnrichmentTests(CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task GetOrder_ReturnsEnrichedNames_NotIds()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var customerId = await client.CreateCustomerAsync("Enrich Co.");

        var create = await client.PostAsJsonAsync("/api/orders", new
        {
            customerId,
            details = new[] { new { productId = 1, orderQuantities = 1, discount = 0 } },
        });
        create.StatusCode.ShouldBe(HttpStatusCode.Created);
        var orderId = (await create.ReadJsonAsync()).GetProperty("orderId").GetInt32();

        var root = await (await client.GetAsync($"/api/orders/{orderId}")).ReadJsonAsync();
        root.GetProperty("customerName").GetString().ShouldBe("Enrich Co.");          // JOIN customers
        root.GetProperty("employeeName").GetString().ShouldBe(AuthHelper.SeedEmployeeName); // 建立者 = acting

        var line = root.GetProperty("details")[0];
        line.GetProperty("productName").GetString().ShouldNotBeNullOrEmpty();          // JOIN products
        line.GetProperty("unitPrice").GetDecimal().ShouldBeGreaterThan(0);
    }
}
