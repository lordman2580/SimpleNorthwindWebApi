using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SimpleNorthwind.Application.ApiLogs;
using SimpleNorthwind.Application.Auth;
using SimpleNorthwind.Application.Common;
using SimpleNorthwind.Application.Customers;
using SimpleNorthwind.Application.Dashboard;
using SimpleNorthwind.Application.Employees;
using SimpleNorthwind.Application.Orders;
using SimpleNorthwind.Application.Products;

namespace SimpleNorthwind.WebApi.Web.Http;

/// <summary>
/// MVC UI 對「自身」<c>/api/*</c> 的 typed loopback client（單一事實 + 稽核重用，見 19-前端架構與整合 §4）。
/// 透過 <see cref="BearerTimeZoneHandler"/> 自動帶 token 與時區；回傳 <see cref="ApiResult{T}"/>（非 2xx 解析 ProblemDetails）。
/// </summary>
public sealed class NorthwindApiClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    private static JsonSerializerOptions CreateJsonOptions()
    {
        // camelCase + 大小寫不敏感（對齊 API 輸出）；日期用本地字串格式 converter（見 LocalDateTimeStringConverter）。
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new LocalDateTimeStringConverter());
        options.Converters.Add(new NullableLocalDateTimeStringConverter());
        return options;
    }

    // ---- 登入（F1） ----

    /// <summary>以員工編號 + 密碼登入，成功回傳 JWT 字串。</summary>
    public Task<ApiResult<string>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
        => PostForTokenAsync("api/auth/login", request, cancellationToken);

    /// <summary>以姓名（first + last，case-sensitive）+ 密碼登入，成功回傳 JWT 字串。</summary>
    public Task<ApiResult<string>> LoginByNameAsync(LoginByNameRequest request, CancellationToken cancellationToken)
        => PostForTokenAsync("api/auth/login-by-name", request, cancellationToken);

    // ---- 訂單（F2） ----

    public Task<ApiResult<IReadOnlyList<OrderDto>>> ListOrdersAsync(CancellationToken ct)
        => GetAsync<IReadOnlyList<OrderDto>>("api/orders", ct);

    public Task<ApiResult<OrderDto>> GetOrderAsync(int id, CancellationToken ct)
        => GetAsync<OrderDto>($"api/orders/{id}", ct);

    public Task<ApiResult<OrderDto>> CreateOrderAsync(CreateOrderRequest request, CancellationToken ct)
        => PostAsync<CreateOrderRequest, OrderDto>("api/orders", request, ct);

    public Task<ApiResult<OrderDto>> UpdateOrderAsync(int id, UpdateOrderRequest request, CancellationToken ct)
        => PutAsync<UpdateOrderRequest, OrderDto>($"api/orders/{id}", request, ct);

    public Task<ApiResult<bool>> CancelOrderAsync(int id, CancellationToken ct)
        => DeleteAsync($"api/orders/{id}", ct);

    // ---- 客戶（F2） ----

    public Task<ApiResult<IReadOnlyList<CustomerDto>>> ListCustomersAsync(CancellationToken ct)
        => GetAsync<IReadOnlyList<CustomerDto>>("api/customers", ct);

    public Task<ApiResult<CustomerDto>> GetCustomerAsync(int id, CancellationToken ct)
        => GetAsync<CustomerDto>($"api/customers/{id}", ct);

    public Task<ApiResult<CustomerDto>> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken ct)
        => PostAsync<CreateCustomerRequest, CustomerDto>("api/customers", request, ct);

    public Task<ApiResult<CustomerDto>> UpdateCustomerAsync(int id, UpdateCustomerRequest request, CancellationToken ct)
        => PutAsync<UpdateCustomerRequest, CustomerDto>($"api/customers/{id}", request, ct);

    public Task<ApiResult<bool>> DeleteCustomerAsync(int id, CancellationToken ct)
        => DeleteAsync($"api/customers/{id}", ct);

    // ---- 產品 / 員工（F2 唯讀） ----

    public Task<ApiResult<PagedResult<ProductDto>>> ListProductsAsync(
        int page, int pageSize, string? category, string? sortBy, bool desc, CancellationToken ct)
    {
        var query = new List<string>
        {
            $"page={page}",
            $"pageSize={pageSize}",
            $"desc={desc.ToString().ToLowerInvariant()}"
        };
        if (!string.IsNullOrWhiteSpace(category)) query.Add($"category={Uri.EscapeDataString(category)}");
        if (!string.IsNullOrWhiteSpace(sortBy)) query.Add($"sortBy={Uri.EscapeDataString(sortBy)}");
        return GetAsync<PagedResult<ProductDto>>($"api/products?{string.Join('&', query)}", ct);
    }

    public Task<ApiResult<IReadOnlyList<EmployeeDto>>> ListEmployeesAsync(CancellationToken ct)
        => GetAsync<IReadOnlyList<EmployeeDto>>("api/employees", ct);

    // ---- 稽核（F2 唯讀） ----

    public Task<ApiResult<PagedResult<ApiLogDto>>> ListApiLogsAsync(
        int? userId, string? method, bool onlyErrors, DateTime? fromUtc, DateTime? toUtc,
        int page, int pageSize, CancellationToken ct)
    {
        var query = new List<string>
        {
            $"page={page}",
            $"pageSize={pageSize}",
            $"onlyErrors={onlyErrors.ToString().ToLowerInvariant()}"
        };
        if (userId is not null) query.Add($"userId={userId}");
        if (!string.IsNullOrWhiteSpace(method)) query.Add($"method={Uri.EscapeDataString(method)}");
        if (fromUtc is not null) query.Add($"from={Uri.EscapeDataString(fromUtc.Value.ToString("o", CultureInfo.InvariantCulture))}");
        if (toUtc is not null) query.Add($"to={Uri.EscapeDataString(toUtc.Value.ToString("o", CultureInfo.InvariantCulture))}");
        return GetAsync<PagedResult<ApiLogDto>>($"api/apilogs?{string.Join('&', query)}", ct);
    }

    // ---- Dashboard（F2） ----

    public Task<ApiResult<DashboardSummaryDto>> GetDashboardSummaryAsync(CancellationToken ct)
        => GetAsync<DashboardSummaryDto>("api/dashboard/summary", ct);

    // ---- 泛型傳輸 helper ----

    private async Task<ApiResult<T>> GetAsync<T>(string path, CancellationToken ct)
    {
        using var response = await httpClient.GetAsync(path, ct).ConfigureAwait(false);
        return await ReadResultAsync<T>(response, ct).ConfigureAwait(false);
    }

    private async Task<ApiResult<TResponse>> PostAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken ct)
    {
        using var response = await httpClient.PostAsJsonAsync(path, body, JsonOptions, ct).ConfigureAwait(false);
        return await ReadResultAsync<TResponse>(response, ct).ConfigureAwait(false);
    }

    private async Task<ApiResult<TResponse>> PutAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken ct)
    {
        using var response = await httpClient.PutAsJsonAsync(path, body, JsonOptions, ct).ConfigureAwait(false);
        return await ReadResultAsync<TResponse>(response, ct).ConfigureAwait(false);
    }

    private async Task<ApiResult<bool>> DeleteAsync(string path, CancellationToken ct)
    {
        using var response = await httpClient.DeleteAsync(path, ct).ConfigureAwait(false);
        return response.IsSuccessStatusCode
            ? ApiResult<bool>.Success(true, response.StatusCode)
            : await ToFailureAsync<bool>(response, ct).ConfigureAwait(false);
    }

    private static async Task<ApiResult<T>> ReadResultAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
            return await ToFailureAsync<T>(response, ct).ConfigureAwait(false);

        var value = await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct).ConfigureAwait(false);
        return value is null
            ? ApiResult<T>.Failure(HttpStatusCode.BadGateway, "API 回應無法解析。", null)
            : ApiResult<T>.Success(value, response.StatusCode);
    }

    private async Task<ApiResult<string>> PostForTokenAsync<TRequest>(string path, TRequest body, CancellationToken ct)
    {
        using var response = await httpClient.PostAsJsonAsync(path, body, JsonOptions, ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
            return await ToFailureAsync<string>(response, ct).ConfigureAwait(false);

        var login = await response.Content.ReadFromJsonAsync<LoginTokenResponse>(JsonOptions, ct).ConfigureAwait(false);
        return login is null || string.IsNullOrEmpty(login.Token)
            ? ApiResult<string>.Failure(HttpStatusCode.BadGateway, "登入回應無法解析。", null)
            : ApiResult<string>.Success(login.Token);
    }

    /// <summary>非 2xx 回應 → 解析 ProblemDetails 轉成 <see cref="ApiResult{T}"/> 失敗（解析失敗時保留狀態碼）。</summary>
    private static async Task<ApiResult<T>> ToFailureAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        ProblemDetails? problem = null;
        try
        {
            problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions, ct).ConfigureAwait(false);
        }
        catch (JsonException) { /* 非 ProblemDetails（如空 body challenge）→ 僅保留狀態碼 */ }
        catch (NotSupportedException) { /* 非 JSON content-type → 僅保留狀態碼 */ }

        return ApiResult<T>.Failure(response.StatusCode, problem?.Title, problem?.Detail);
    }

    /// <summary>登入回應傳輸型別：只取 token；到期時間由呼叫端解析 JWT exp（見 AccountUiController）。</summary>
    private sealed record LoginTokenResponse(string Token, string? ExpiresAt);
}
