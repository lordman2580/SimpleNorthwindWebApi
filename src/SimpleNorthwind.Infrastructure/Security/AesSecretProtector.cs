using System.Security.Cryptography;
using System.Text;

namespace SimpleNorthwind.Infrastructure.Security;

/// <summary>
/// AES-256-GCM 加解密。輸出格式 base64( nonce(12) | tag(16) | ciphertext )。
/// 金鑰為 32 bytes（base64），來自環境變數 APP_SECRET_KEY（prod）或 gitignored secret.decryption.key（dev）。
/// dev 連線字串為明文（無 enc: 前綴）時不會呼叫到本類別，故金鑰缺漏僅在實際 Encrypt/Decrypt 時才報錯。
/// </summary>
internal sealed class AesSecretProtector(string? keyBase64) : ISecretProtector
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly byte[]? _key = string.IsNullOrWhiteSpace(keyBase64) ? null : Convert.FromBase64String(keyBase64);

    public string Encrypt(string plaintext)
    {
        var key = RequireKey();
        var plain = Encoding.UTF8.GetBytes(plaintext);
        var nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);
        var cipher = new byte[plain.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plain, cipher, tag);

        var output = new byte[NonceSize + TagSize + cipher.Length];
        Buffer.BlockCopy(nonce, 0, output, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, output, NonceSize, TagSize);
        Buffer.BlockCopy(cipher, 0, output, NonceSize + TagSize, cipher.Length);
        return Convert.ToBase64String(output);
    }

    public string Decrypt(string ciphertext)
    {
        var key = RequireKey();
        var input = Convert.FromBase64String(ciphertext);
        var nonce = input.AsSpan(0, NonceSize);
        var tag = input.AsSpan(NonceSize, TagSize);
        var cipher = input.AsSpan(NonceSize + TagSize);
        var plain = new byte[cipher.Length];

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, cipher, tag, plain);
        return Encoding.UTF8.GetString(plain);
    }

    private byte[] RequireKey() => _key is { Length: 32 }
        ? _key
        : throw new InvalidOperationException(
            "AES-256 金鑰未設定或長度不符（需 32 bytes base64）。請設定環境變數 APP_SECRET_KEY 或 secret.decryption.key。");
}
