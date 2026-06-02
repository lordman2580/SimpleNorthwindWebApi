using SimpleNorthwind.Domain.Entities;

namespace SimpleNorthwind.Application.Abstractions.Security;

public interface IJwtTokenService
{
    /// <summary>簽發 JWT。回傳 token 與到期 UTC 時刻（內部值，故保留 Utc 命名）。</summary>
    (string Token, DateTime ExpiresAtUtc) CreateToken(Employee employee);
}
