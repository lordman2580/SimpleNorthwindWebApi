using System.Net;
using System.Net.Http.Json;
using Shouldly;

namespace SimpleNorthwind.E2E.Tests;

[Collection(E2ECollection.Name)]
public sealed class OrdersEndpointTests(CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task CreateOrder_WithTwoProducts_Returns201AndDecrementsStock()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var customerId = await client.CreateCustomerAsync("Orders Co. A");

        const int productA = 50;
        const int productB = 51;
        const int qtyA = 3;
        const int qtyB = 2;
        var stockA0 = await factory.GetProductStockAsync(productA);
        var stockB0 = await factory.GetProductStockAsync(productB);

        var response = await client.PostAsJsonAsync("/api/orders", new
        {
            customerId,
            details = new[]
            {
                new { productId = productA, orderQuantities = qtyA, discount = 0 },
                new { productId = productB, orderQuantities = qtyB, discount = 0 },
            },
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var root = await response.ReadJsonAsync();
        root.GetProperty("orderId").GetInt32().ShouldBeGreaterThan(0);
        root.GetProperty("details").GetArrayLength().ShouldBe(2);

        (await factory.GetProductStockAsync(productA)).ShouldBe(stockA0 - qtyA);
        (await factory.GetProductStockAsync(productB)).ShouldBe(stockB0 - qtyB);
    }

    [Fact]
    public async Task CreateOrder_Oversell_Returns400AndStockUnchanged()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var customerId = await client.CreateCustomerAsync("Orders Co. Oversell");

        const int productId = 60;
        var stock0 = await factory.GetProductStockAsync(productId);

        var response = await client.PostAsJsonAsync("/api/orders", new
        {
            customerId,
            details = new[] { new { productId, orderQuantities = 1_000_000, discount = 0 } },
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await factory.GetProductStockAsync(productId)).ShouldBe(stock0); // 交易 rollback，庫存不變
    }

    [Fact]
    public async Task UpdateOrder_NoChanges_Returns400AndStockUnchanged()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var customerId = await client.CreateCustomerAsync("Orders Co. NoOp");

        const int productId = 70;
        var create = await client.PostAsJsonAsync("/api/orders", new
        {
            customerId,
            details = new[] { new { productId, orderQuantities = 2, discount = 0 } },
        });
        create.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await create.ReadJsonAsync();
        var orderId = created.GetProperty("orderId").GetInt32();
        var version = created.GetProperty("details")[0].GetProperty("version").GetInt32();
        var stockAfterCreate = await factory.GetProductStockAsync(productId);

        // 相同資料 + 正確 version → 未修改 → 400，不寫 DB（庫存不變）
        var update = await client.PutAsJsonAsync($"/api/orders/{orderId}", new
        {
            details = new[] { new { productId, orderQuantities = 2, discount = 0, version } },
        });

        update.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await factory.GetProductStockAsync(productId)).ShouldBe(stockAfterCreate);
    }

    [Fact]
    public async Task UpdateOrder_ChangedQuantity_WithCurrentVersion_Returns200AndAdjustsStock()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var customerId = await client.CreateCustomerAsync("Orders Co. Update");

        const int productId = 71;
        var create = await client.PostAsJsonAsync("/api/orders", new
        {
            customerId,
            details = new[] { new { productId, orderQuantities = 2, discount = 0 } },
        });
        create.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await create.ReadJsonAsync();
        var orderId = created.GetProperty("orderId").GetInt32();
        var version = created.GetProperty("details")[0].GetProperty("version").GetInt32();
        var stockAfterCreate = await factory.GetProductStockAsync(productId);

        // 數量 2 → 5 + 正確 version → 200，再扣 3
        var update = await client.PutAsJsonAsync($"/api/orders/{orderId}", new
        {
            details = new[] { new { productId, orderQuantities = 5, discount = 0, version } },
        });

        update.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await factory.GetProductStockAsync(productId)).ShouldBe(stockAfterCreate - 3);
    }

    [Fact]
    public async Task UpdateOrder_ChangedData_WithStaleVersion_Returns409AndStockUnchanged()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var customerId = await client.CreateCustomerAsync("Orders Co. Conflict");

        const int productId = 72;
        var create = await client.PostAsJsonAsync("/api/orders", new
        {
            customerId,
            details = new[] { new { productId, orderQuantities = 2, discount = 0 } },
        });
        create.StatusCode.ShouldBe(HttpStatusCode.Created);
        var orderId = (await create.ReadJsonAsync()).GetProperty("orderId").GetInt32();
        var stockAfterCreate = await factory.GetProductStockAsync(productId);

        // 資料有變（2→5）但帶過時 version → 樂觀並行衝突 409，rollback 庫存不變
        var update = await client.PutAsJsonAsync($"/api/orders/{orderId}", new
        {
            details = new[] { new { productId, orderQuantities = 5, discount = 0, version = 999 } },
        });

        update.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await factory.GetProductStockAsync(productId)).ShouldBe(stockAfterCreate);
    }

    [Fact]
    public async Task UpdateOrder_StaleVersion_TakesPrecedenceOverNoChanges_Returns409()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var customerId = await client.CreateCustomerAsync("Orders Co. VerFirst");

        const int productId = 73;
        var create = await client.PostAsJsonAsync("/api/orders", new
        {
            customerId,
            details = new[] { new { productId, orderQuantities = 2, discount = 0 } },
        });
        create.StatusCode.ShouldBe(HttpStatusCode.Created);
        var orderId = (await create.ReadJsonAsync()).GetProperty("orderId").GetInt32();

        // 資料「相同」(qty 2) 但帶過時 version → version 優先於 no-op → 409（非 400）
        var update = await client.PutAsJsonAsync($"/api/orders/{orderId}", new
        {
            details = new[] { new { productId, orderQuantities = 2, discount = 0, version = 999 } },
        });

        update.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }
}
