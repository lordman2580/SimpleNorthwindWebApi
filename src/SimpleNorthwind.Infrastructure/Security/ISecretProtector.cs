namespace SimpleNorthwind.Infrastructure.Security;

/// <summary>可逆對稱加密（AES-256-GCM）。用於連線字串、JWT secret 等可還原機密。</summary>
internal interface ISecretProtector
{
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
}
