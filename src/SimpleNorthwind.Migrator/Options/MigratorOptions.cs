namespace SimpleNorthwind.Migrator.Options;

/// <summary>
/// Migrator 設定。連線字串的 Initial Catalog 即目標資料庫名（單一事實來源），由 runner 解析。
/// </summary>
public sealed class MigratorOptions
{
    public required string ConnectionString { get; init; }
}
