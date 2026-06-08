using System.Globalization;

namespace SimpleNorthwind.WebApi.Web.Helpers;

/// <summary>
/// 列表頁查詢字串組裝（單一事實）：保留目前過濾條件、覆寫指定鍵；null/空白值略過、值一律 URL 編碼。
/// 取代 Orders/Customers/Products/ApiLogs 各自手拼（部分未 escape）的 query 建構
/// （見 29-前端共用模組抽取稽核 #5）。
/// </summary>
public static class QueryString
{
    /// <summary>以 (key, value) 配對組成 <c>path?k=v&amp;...</c>；bool → true/false，其餘以 InvariantCulture 字串化。</summary>
    public static string Build(string path, params (string Key, object? Value)[] values)
    {
        var parts = new List<string>(values.Length);
        foreach (var (key, value) in values)
        {
            var text = value switch
            {
                null => null,
                bool b => b ? "true" : "false",
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString(),
            };
            if (!string.IsNullOrWhiteSpace(text))
                parts.Add($"{key}={Uri.EscapeDataString(text)}");
        }
        return parts.Count == 0 ? path : $"{path}?{string.Join('&', parts)}";
    }
}
