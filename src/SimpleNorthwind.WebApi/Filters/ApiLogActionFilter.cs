using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.IdentityModel.JsonWebTokens;
using SimpleNorthwind.Application.Abstractions.Persistence;

namespace SimpleNorthwind.WebApi.Filters;

/// <summary>
/// 全域稽核：每個 action 執行後寫入 api_logs（guid / user_id / actions / action_detail / summary_date）。
/// 敏感欄位（password / token / secret / authorization）redact 為 ***。寫入失敗不影響主流程。
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

        await next().ConfigureAwait(false);

        try
        {
            await apiLogs.WriteAsync(Guid.NewGuid(), userId, actions, detail, DateTime.UtcNow, context.HttpContext.RequestAborted)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "寫入 api_logs 失敗（不影響主流程）。");
        }
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
}
