using SimpleNorthwind.Domain.Entities;

namespace SimpleNorthwind.Application.Abstractions.Security;

/// <summary>包裝 PasswordHasher&lt;Employee&gt;（PBKDF2-HMAC-SHA256），不自寫雜湊。</summary>
public interface IPasswordHashing
{
    /// <summary>驗證明文密碼是否符合已雜湊密碼。</summary>
    bool Verify(Employee employee, string hashedPassword, string providedPassword);

    /// <summary>產生密碼雜湊（供建立 / 變更密碼用）。</summary>
    string Hash(Employee employee, string password);
}
