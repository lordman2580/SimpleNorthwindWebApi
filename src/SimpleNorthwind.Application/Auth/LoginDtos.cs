namespace SimpleNorthwind.Application.Auth;

public sealed record LoginRequest(int EmployeeId, string Password);

/// <summary>以姓名（first + last）+ 密碼登入，**case-sensitive**（UD11）。與 LoginRequest 並存。</summary>
public sealed record LoginByNameRequest(string FirstName, string LastName, string Password);

/// <summary>
/// ExpiresAt 不帶 Utc 後綴：序列化經 client-local converter → 輸出為呼叫端本地時區。
/// 內部持有的是 UTC 時刻（由 IJwtTokenService 提供）。
/// </summary>
public sealed record LoginResponse(string Token, DateTime ExpiresAt);
