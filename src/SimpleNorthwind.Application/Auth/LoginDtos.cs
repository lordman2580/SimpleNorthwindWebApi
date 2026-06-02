namespace SimpleNorthwind.Application.Auth;

public sealed record LoginRequest(int EmployeeId, string Password);

/// <summary>
/// ExpiresAt 不帶 Utc 後綴：序列化經 client-local converter → 輸出為呼叫端本地時區。
/// 內部持有的是 UTC 時刻（由 IJwtTokenService 提供）。
/// </summary>
public sealed record LoginResponse(string Token, DateTime ExpiresAt);
