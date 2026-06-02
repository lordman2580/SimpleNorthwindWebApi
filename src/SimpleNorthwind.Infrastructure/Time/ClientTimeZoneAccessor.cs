namespace SimpleNorthwind.Infrastructure.Time;

/// <summary>
/// per-request 的呼叫端時區（middleware 由 X-Time-Zone header 設定）。
/// 缺漏時退回 Default（啟動時由 App:DefaultTimeZone 設定，非 UTC）。
/// 序列化 converter 讀取此處決定輸出/輸入時區。
/// </summary>
public static class ClientTimeZoneAccessor
{
    private static readonly AsyncLocal<TimeZoneInfo?> CurrentTimeZone = new();
    private static TimeZoneInfo _default = TimeZoneInfo.Utc;

    public static TimeZoneInfo Current
    {
        get => CurrentTimeZone.Value ?? _default;
        set => CurrentTimeZone.Value = value;
    }

    public static TimeZoneInfo Default => _default;

    /// <summary>啟動時設定預設時區（來自 App:DefaultTimeZone）。</summary>
    public static void SetDefault(TimeZoneInfo timeZone) => _default = timeZone;
}
