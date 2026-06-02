using Microsoft.AspNetCore.Identity;
using SimpleNorthwind.Application.Abstractions.Security;
using SimpleNorthwind.Domain.Entities;

namespace SimpleNorthwind.Infrastructure.Security;

/// <summary>包裝 PasswordHasher&lt;Employee&gt;（PBKDF2-HMAC-SHA256），不自寫雜湊。</summary>
internal sealed class PasswordHashing : IPasswordHashing
{
    private readonly PasswordHasher<Employee> _hasher = new();

    public bool Verify(Employee employee, string hashedPassword, string providedPassword)
    {
        var result = _hasher.VerifyHashedPassword(employee, hashedPassword, providedPassword);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }

    public string Hash(Employee employee, string password) => _hasher.HashPassword(employee, password);
}
