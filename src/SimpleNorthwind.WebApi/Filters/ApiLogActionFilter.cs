using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.IdentityModel.JsonWebTokens;
using SimpleNorthwind.Application.Abstractions.Persistence;

namespace SimpleNorthwind.WebApi.Filters;

/// <summary>
/// 全域稽核：每個 action 執行後寫入 api_logs（guid / user_id / actions / action_detail /
/// response_status / response_result / summary_date）。敏感欄位（password / token / secret /
/// authorization）redact 為 ***。寫入失敗不影響主流程。
/// </summary>
public sealed class ApiLogActionFilter(IApiLogRepository apiLogs, ILogger<ApiLogActionFilter> logger) : IAsyncActionFilter
{
    private static readonly string[] SensitiveKeys = ["password", "token", "secret", "authorization"];

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // 先記下參數（行為發生於 action 執行前後皆可，這裡在執行後寫入以涵蓋驗證失敗的呼叫）
        var detail = BuildDetail(context);
        var actions = BuildActionName(context);
        var userId = ResolveUserId(context);

        var executed = await next().ConfigureAwait(false);

        var (status, resultBody) = ExtractResponse(executed);

        try
        {
            await apiLogs.WriteAsync(Guid.NewGuid(), userId, actions, detail, status, resultBody,
                DateTime.UtcNow, context.HttpContext.RequestAborted).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "寫入 api_logs 失敗（不影響主流程）。");
        }
    }

    /// <summary>由 action 結果取 HTTP 狀態碼與回應內容（redact 後）。NoContent 等無 body → null。</summary>
    private static (int? Status, string? Body) ExtractResponse(ActionExecutedContext executed)
    {
        var status = (executed.Result as IStatusCodeActionResult)?.StatusCode
                     ?? executed.HttpContext.Response.StatusCode;

        string? body = executed.Result is ObjectResult { Value: not null } obj
            ? SerializeRedacted(obj.Value)
            : null;

        return (status, body);
    }

    private static int? ResolveUserId(ActionExecutingContext context)
    {
        var sub = context.HttpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return int.TryParse(sub, out var id) ? id : null;
    }

    private static string BuildActionName(ActionExecutingContext context)
    {
        var controller = (context.ActionDescriptor as ControllerActionDescriptor)?.ControllerName ?? "Unknown";
        var action = (context.ActionDescriptor as ControllerActionDescriptor)?.ActionName ?? context.HttpContext.Request.Method;
        return $"{controller}.{action}";
    }

    private static string BuildDetail(ActionExecutingContext context)
    {
        var request = context.HttpContext.Request;
        var builder = new StringBuilder();
        builder.Append(request.Method).Append(' ').Append(request.Path);
        if (request.QueryString.HasValue)
            builder.Append(request.QueryString.Value);

        var args = context.ActionArguments
            .Where(kv => kv.Value is not null and not CancellationToken)
            .ToDictionary(kv => kv.Key, kv => Redact(kv.Value!));

        if (args.Count > 0)
            builder.Append(" | args=").Append(JsonSerializer.Serialize(args));

        return builder.ToString();
    }

    private static object? Redact(object value)
    {
        if (value is string or bool or int or long or decimal or double or DateTime or Guid)
            return value;

        var redacted = new Dictionary<string, object?>();
        foreach (var property in value.GetType().GetProperties())
        {
            var isSensitive = SensitiveKeys.Any(key => property.Name.Contains(key, StringComparison.OrdinalIgnoreCase));
            redacted[property.Name] = isSensitive ? "***" : property.GetValue(value);
        }
        return redacted;
    }

    /// <summary>序列化回應並 redact 敏感欄位（支援巢狀物件與陣列，故 List 回應也安全）。</summary>
    private static string? SerializeRedacted(object value)
    {
        var node = JsonSerializer.SerializeToNode(value);
        RedactNode(node);
        return node?.ToJsonString();
    }

    private static void RedactNode(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var (key, child) in obj.ToList())
                {
                    if (SensitiveKeys.Any(k => key.Contains(k, StringComparison.OrdinalIgnoreCase)))
                        obj[key] = "***";
                    else
                        RedactNode(child);
                }
                break;
            case JsonArray arr:
                foreach (var item in arr)
                    RedactNode(item);
                break;
        }
    }
}
