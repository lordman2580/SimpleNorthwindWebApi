using System.Net;
using System.Net.Http.Json;
using Shouldly;

namespace SimpleNorthwind.E2E.Tests;

[Collection(E2ECollection.Name)]
public sealed class TimeZoneTests(CustomWebApplicationFactory factory)
{
    // 種子訂單 1 的 order_date = 2024-01-02 00:00:00 (UTC)。
    [Theory]
    [InlineData("UTC", "2024-01-02 00:00:00")]
    [InlineData("Asia/Taipei", "2024-01-02 08:00:00")]
    [InlineData("Asia/Tokyo", "2024-01-02 09:00:00")]
    [InlineData(null, "2024-01-02 08:00:00")] // 無 header → 退回預設 Asia/Taipei（非 UTC）
    public async Task GetOrder_OutputsOrderDate_InClientTimeZone(string? timeZone, string expected)
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/orders/1");
        if (timeZone is not null)
            request.Headers.Add("X-Time-Zone", timeZone);
        var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.ReadJsonAsync()).GetProperty("orderDate").GetString().ShouldBe(expected);
    }

    [Fact]
    public async Task CustomerDate_InputLocal_StoredAsUtc_RoundTrips()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var id = await client.CreateCustomerAsync("TimeZone Co.");

        // 以 Asia/Taipei (UTC+8) 輸入 09:00 → DB 應存 UTC 01:00
        using var put = new HttpRequestMessage(HttpMethod.Put, $"/api/customers/{id}")
        {
            Content = JsonContent.Create(new
            {
                companyName = "TimeZone Co.",
                contactNumber = "02-2222-2222",
                contactTitle = "Owner",
                isOutContacted = true,
                outContactedDate = "2024-06-01 09:00:00",
            }),
        };
        put.Headers.Add("X-Time-Zone", "Asia/Taipei");
        (await client.SendAsync(put)).StatusCode.ShouldBe(HttpStatusCode.OK);

        // 以 UTC 讀回 → 應為 01:00（證明 input 本地→存 UTC→output 依時區）
        using var get = new HttpRequestMessage(HttpMethod.Get, $"/api/customers/{id}");
        get.Headers.Add("X-Time-Zone", "UTC");
        var response = await client.SendAsync(get);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.ReadJsonAsync()).GetProperty("outContactedDate").GetString()
            .ShouldBe("2024-06-01 01:00:00");
    }
}
