using System.Globalization;
using SimpleNorthwind.Infrastructure.Time;

namespace SimpleNorthwind.WebApi.Middleware;

/// <summary>
/// 讀取 X-Time-Zone header（IANA id 或 ±hh:mm offset），設定 per-request <see cref="ClientTimeZoneAccessor.Current"/>。
/// 缺漏 / 無法解析 → 退回 <see cref="ClientTimeZoneAccessor.Default"/>（啟動時由 App:DefaultTimeZone 設定）。
/// </summary>
public sealed class ClientTimeZoneMiddleware(RequestDelegate next)
{
    private const string HeaderName = "X-Time-Zone";

    public async Task InvokeAsync(HttpContext context)
    {
        var header = context.Request.Headers[HeaderName].ToString();
        ClientTimeZoneAccessor.Current = Resolve(header);
        await next(context).ConfigureAwait(false);
    }

    private static TimeZoneInfo Resolve(string? header)
    {
        if (string.IsNullOrWhiteSpace(header))
            return ClientTimeZoneAccessor.Default;

        var value = header.Trim();

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(value);
        }
        catch (TimeZoneNotFoundException) { }
        catch (InvalidTimeZoneException) { }

        return TryParseOffset(value, out var tz) ? tz : ClientTimeZoneAccessor.Default;
    }

    private static bool TryParseOffset(string value, out TimeZoneInfo timeZone)
    {
        timeZone = TimeZoneInfo.Utc;

        var sign = value[0] switch { '+' => 1, '-' => -1, _ => 0 };
        if (sign == 0)
            return false;

        if (!TimeSpan.TryParseExact(value[1..], @"hh\:mm", CultureInfo.InvariantCulture, out var magnitude))
            return false;

        var offset = sign < 0 ? -magnitude : magnitude;
        if (offset < TimeSpan.FromHours(-14) || offset > TimeSpan.FromHours(14))
            return false;

        var id = $"UTC{value}";
        timeZone = TimeZoneInfo.CreateCustomTimeZone(id, offset, id, id);
        return true;
    }
}
