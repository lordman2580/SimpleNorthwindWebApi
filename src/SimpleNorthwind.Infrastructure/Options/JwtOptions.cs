namespace SimpleNorthwind.Infrastructure.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiresMinutes { get; set; } = 60;

    /// <summary>HS256 簽章金鑰（dev 明文於 User Secrets；prod enc: 密文）。</summary>
    public string Secret { get; set; } = string.Empty;
}
