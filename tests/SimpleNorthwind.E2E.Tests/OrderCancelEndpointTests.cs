using System.Net;
using System.Net.Http.Json;
using Shouldly;

namespace SimpleNorthwind.E2E.Tests;

[Collection(E2ECollection.Name)]
public sealed class OrderCancelEndpointTests(CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task CancelOrder_NotPaidoff_Returns204AndRestoresStock()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var customerId = await client.CreateCustomerAsync("Cancel Co.");

        const int productId = 80;
        const int qty = 4;
        var stock0 = await factory.GetProductStockAsync(productId);

        var create = await client.PostAsJsonAsync("/api/orders", new
        {
            customerId,
            details = new[] { new { productId, orderQuantities = qty, discount = 0 } },
        });
        create.StatusCode.ShouldBe(HttpStatusCode.Created);
        var orderId = (await create.ReadJsonAsync()).GetProperty("orderId").GetInt32();
        (await factory.GetProductStockAsync(productId)).ShouldBe(stock0 - qty);

        var cancel = await client.DeleteAsync($"/api/orders/{orderId}");

        cancel.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await factory.GetProductStockAsync(productId)).ShouldBe(stock0); // 庫存還原
    }

    [Fact]
    public async Task CancelOrder_Paidoff_Returns409()
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        // 種子訂單 17 為已付清（is_paidoff，rn % 17 = 0）→ 不可取消
        var response = await client.DeleteAsync("/api/orders/17");

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }
}
