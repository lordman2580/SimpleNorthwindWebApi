using System.Net;
using Shouldly;

namespace SimpleNorthwind.E2E.Tests;

// F4.1：產品唯讀端點（分頁 + CategoryName 非 id + 排序）。
[Collection(E2ECollection.Name)]
public sealed class ProductsEndpointTests(CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task ListProducts_Paged_ReturnsCategoryNameNotId()
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        var res = await client.GetAsync("/api/products?page=1&pageSize=5");
        res.StatusCode.ShouldBe(HttpStatusCode.OK);

        var root = await res.ReadJsonAsync();
        root.GetProperty("page").GetInt32().ShouldBe(1);
        root.GetProperty("pageSize").GetInt32().ShouldBe(5);
        root.GetProperty("totalCount").GetInt32().ShouldBeGreaterThan(5);

        var items = root.GetProperty("items");
        items.GetArrayLength().ShouldBe(5);
        items[0].GetProperty("productName").GetString().ShouldNotBeNullOrEmpty();
        items[0].GetProperty("categoryName").GetString().ShouldNotBeNullOrEmpty(); // 名稱非 category_id
    }

    [Fact]
    public async Task ListProducts_SortByPriceDesc_OrdersDescending()
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        var root = await (await client.GetAsync("/api/products?pageSize=20&sortBy=price&desc=true")).ReadJsonAsync();

        var prev = decimal.MaxValue;
        foreach (var item in root.GetProperty("items").EnumerateArray())
        {
            var price = item.GetProperty("unitPrice").GetDecimal();
            price.ShouldBeLessThanOrEqualTo(prev);
            prev = price;
        }
    }
}
