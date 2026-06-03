using System.Security.Cryptography;
using Shouldly;
using SimpleNorthwind.Infrastructure.Security;

namespace SimpleNorthwind.Infrastructure.UnitTests;

public class AesSecretProtectorTests
{
    private static string GenerateKey()
    {
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);
        return Convert.ToBase64String(key);
    }

    [Fact]
    public void EncryptThenDecrypt_RoundTrips()
    {
        const string original = "connection-string;Pwd=1";
        var protector = new AesSecretProtector(GenerateKey());

        var ciphertext = protector.Encrypt(original);
        var decrypted = protector.Decrypt(ciphertext);

        decrypted.ShouldBe(original);
        ciphertext.ShouldNotBe(original);
    }

    [Fact]
    public void Encrypt_SameText_TwiceSameKey_ProducesDifferentCiphertext()
    {
        const string original = "connection-string;Pwd=1";
        var protector = new AesSecretProtector(GenerateKey());

        var c1 = protector.Encrypt(original);
        var c2 = protector.Encrypt(original);

        // random nonce means the two outputs must differ
        c1.ShouldNotBe(c2);
    }

    [Fact]
    public void Decrypt_WithDifferentKey_Throws()
    {
        const string original = "connection-string;Pwd=1";
        var protectorA = new AesSecretProtector(GenerateKey());
        var protectorB = new AesSecretProtector(GenerateKey());

        var ciphertext = protectorA.Encrypt(original);

        Should.Throw<CryptographicException>(() => protectorB.Decrypt(ciphertext));
    }

    [Fact]
    public void Encrypt_WithNoKey_Throws()
    {
        var protector = new AesSecretProtector(null);

        Should.Throw<InvalidOperationException>(() => protector.Encrypt("any text"));
    }

    [Fact]
    public void Decrypt_WithNoKey_Throws()
    {
        var protector = new AesSecretProtector(null);

        Should.Throw<InvalidOperationException>(() => protector.Decrypt("any-base64=="));
    }
}
