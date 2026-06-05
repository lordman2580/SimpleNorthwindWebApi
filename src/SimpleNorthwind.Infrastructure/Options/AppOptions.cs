namespace SimpleNorthwind.Infrastructure.Options;

public sealed class AppOptions
{
    public const string SectionName = "App";

    /// <summary>X-Time-Zone header 缺漏時的預設時區（IANA id）。</summary>
    public string DefaultTimeZone { get; set; } = "Asia/Taipei";

    /// <summary>Dashboard 庫存預警門檻：products.quantities &lt;= 此值視為低庫存（UD14）。</summary>
    public int LowStockThreshold { get; set; } = 10;
}
