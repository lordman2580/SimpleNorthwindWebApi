namespace SimpleNorthwind.WebApi.Web;

/// <summary>
/// 顯示用日期格式常數（單一事實），取代散落各 view 的字面字串
/// （見 29-前端共用模組抽取稽核 #14）。DB 存 UTC、API 邊界轉本地，這裡僅為呈現格式。
/// </summary>
public static class DateFmt
{
    /// <summary>日期：<c>yyyy-MM-dd</c>。</summary>
    public const string Date = "yyyy-MM-dd";

    /// <summary>日期時間：<c>yyyy-MM-dd HH:mm:ss</c>。</summary>
    public const string DateTime = "yyyy-MM-dd HH:mm:ss";
}
