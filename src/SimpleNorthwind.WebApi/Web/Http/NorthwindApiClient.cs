using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SimpleNorthwind.Application.Auth;

namespace SimpleNorthwind.WebApi.Web.Http;

/// <summary>
/// MVC UI 對「自身」<c>/api/*</c> 的 typed loopback client（單一事實 + 稽核重用，見 19-前端架構與整合 §4）。
/// 透過 <see cref="BearerTimeZoneHandler"/> 自動帶 token 與時區。
/// F1 僅實作登入；各資源讀寫方法於 F2 補上。
/// </summary>
public sealed class NorthwindApiClient(HttpClient httpClient)
{
    // JsonSerializerDefaults.Web：camelCase + 大小寫不敏感，對齊 API 既有輸出。
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>以員工編號 + 密碼登入，成功回傳 JWT 字串。</summary>
    public Task<ApiResult<string>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
        => PostForTokenAsync("api/auth/login", request, cancellationToken);

    /// <summary>以姓名（first + last，case-sensitive）+ 密碼登入，成功回傳 JWT 字串。</summary>
    public Task<ApiResult<string>> LoginByNameAsync(LoginByNameRequest request, CancellationToken cancellationToken)
        => PostForTokenAsync("api/auth/login-by-name", request, cancellationToken);

    private async Task<ApiResult<string>> PostForTokenAsync<TRequest>(
        string path, TRequest body, CancellationToken cancellationToken)
    {
        using var response = await httpClient
            .PostAsJsonAsync(path, body, JsonOptions, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
            return await ToFailureAsync<string>(response, cancellationToken).ConfigureAwait(false);

        var login = await response.Content
            .ReadFromJsonAsync<LoginTokenResponse>(JsonOptions, cancellationToken)
            .ConfigureAwait(false);

        return login is null || string.IsNullOrEmpty(login.Token)
            ? ApiResult<string>.Failure(HttpStatusCode.BadGateway, "登入回應無法解析。", null)
            : ApiResult<string>.Success(login.Token);
    }

    /// <summary>非 2xx 回應 → 解析 ProblemDetails 轉成 <see cref="ApiResult{T}"/> 失敗（解析失敗時保留狀態碼）。</summary>
    private static async Task<ApiResult<T>> ToFailureAsync<T>(
        HttpResponseMessage response, CancellationToken cancellationToken)
    {
        ProblemDetails? problem = null;
        try
        {
            problem = await response.Content
                .ReadFromJsonAsync<ProblemDetails>(JsonOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (JsonException)
        {
            // 非 ProblemDetails（如空 body 的 challenge）→ 僅保留狀態碼。
        }
        catch (NotSupportedException)
        {
            // 非 JSON content-type → 僅保留狀態碼。
        }

        return ApiResult<T>.Failure(response.StatusCode, problem?.Title, problem?.Detail);
    }

    /// <summary>
    /// 登入回應傳輸型別：只取 token；<c>ExpiresAt</c> 為 client-local 格式字串，
    /// 到期時間改由呼叫端解析 JWT 的 <c>exp</c>（UTC 權威值），避免時區字串回轉。
    /// </summary>
    private sealed record LoginTokenResponse(string Token, string? ExpiresAt);
}
