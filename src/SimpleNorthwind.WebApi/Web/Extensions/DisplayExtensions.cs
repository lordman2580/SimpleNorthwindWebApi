namespace SimpleNorthwind.WebApi.Web.Extensions;

/// <summary>
/// 呈現層空值顯示輔助：null/空白字串或無值日期統一顯示破折號「—」，取代各 view 重複的三元式
/// （見 29-前端共用模組抽取稽核 #13）。
/// </summary>
public static class DisplayExtensions
{
    /// <summary>字串為 null/空白 → 「—」，否則原值。</summary>
    public static string DisplayOrDash(this string? value)
        => string.IsNullOrWhiteSpace(value) ? "—" : value;

    /// <summary>日期無值 → 「—」，否則以 <paramref name="format"/> 格式化（搭配 <see cref="SimpleNorthwind.WebApi.Web.Helpers.DateFmt"/>）。</summary>
    public static string DisplayOrDash(this DateTime? value, string format)
        => value.HasValue ? value.Value.ToString(format) : "—";
}
