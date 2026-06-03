using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SimpleNorthwind.E2E.Tests;

/// <summary>登入取 token、產生帶 Bearer 的 HttpClient，與測試共用的小工具。</summary>
internal static class AuthHelper
{
    public const int SeedEmployeeId = 1;
    public const string SeedPassword = "P@ssw0rd!";

    public static async Task<string> LoginAndGetTokenAsync(
        HttpClient client, int employeeId = SeedEmployeeId, string password = SeedPassword)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { employeeId, password });
        response.EnsureSuccessStatusCode();
        var root = await response.ReadJsonAsync();
        return root.GetProperty("token").GetString()!;
    }

    /// <summary>建立已登入（帶 Authorization: Bearer）的 HttpClient。</summary>
    public static async Task<HttpClient> CreateAuthenticatedClientAsync(
        this CustomWebApplicationFactory factory, int employeeId = SeedEmployeeId, string password = SeedPassword)
    {
        var client = factory.CreateClient();
        var token = await LoginAndGetTokenAsync(client, employeeId, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>建立一筆客戶，回傳新 customerId（訂單 FK 需要既有客戶）。</summary>
    public static async Task<int> CreateCustomerAsync(this HttpClient client, string companyName)
    {
        var response = await client.PostAsJsonAsync("/api/customers",
            new { companyName, contactNumber = "02-1234-5678", contactTitle = "Owner" });
        response.EnsureSuccessStatusCode();
        var root = await response.ReadJsonAsync();
        return root.GetProperty("customerId").GetInt32();
    }

    /// <summary>
    /// 讀回應為 detached <see cref="JsonElement"/>。
    /// 不直接反序列化成 DTO：日期欄走 client-local converter 輸出為 "yyyy-MM-dd HH:mm:ss"，
    /// 用預設 System.Text.Json 反序列化 DateTime 會失敗，故以 JsonElement 逐欄讀取。
    /// </summary>
    public static async Task<JsonElement> ReadJsonAsync(this HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }
}
