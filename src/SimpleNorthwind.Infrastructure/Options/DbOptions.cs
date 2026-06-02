namespace SimpleNorthwind.Infrastructure.Options;

public sealed class DbOptions
{
    /// <summary>連線字串（dev 明文於 User Secrets；prod 以 enc: 密文，啟動時 PostConfigure 解密）。</summary>
    public string ConnectionString { get; set; } = string.Empty;
}
