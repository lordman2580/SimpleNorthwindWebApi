using System.Net;

namespace SimpleNorthwind.WebApi.Web.Http;

/// <summary>
/// loopback 呼叫結果：成功帶值；失敗帶 <see cref="System.Net.HttpStatusCode"/> 與
/// ProblemDetails 摘要（Title + Detail），讓 UI controller 依狀態碼分流
/// （401 導回登入、409 友善提示…）而不必直接處理 <c>HttpResponseMessage</c>。
/// 見 19-前端架構與整合 §8。
/// </summary>
public sealed class ApiResult<T>
{
    private ApiResult(bool isSuccess, T? value, HttpStatusCode statusCode, string? title, string? detail)
    {
        IsSuccess = isSuccess;
        Value = value;
        StatusCode = statusCode;
        Title = title;
        Detail = detail;
    }

    public bool IsSuccess { get; }

    /// <summary>成功時的內容；失敗時為 <c>default</c>。</summary>
    public T? Value { get; }

    public HttpStatusCode StatusCode { get; }

    /// <summary>失敗時對映 ProblemDetails.Title（領域錯誤碼）。</summary>
    public string? Title { get; }

    /// <summary>失敗時對映 ProblemDetails.Detail（人類可讀訊息）。</summary>
    public string? Detail { get; }

    public bool IsUnauthorized => StatusCode == HttpStatusCode.Unauthorized;

    public static ApiResult<T> Success(T value, HttpStatusCode statusCode = HttpStatusCode.OK)
        => new(true, value, statusCode, null, null);

    public static ApiResult<T> Failure(HttpStatusCode statusCode, string? title, string? detail)
        => new(false, default, statusCode, title, detail);
}
