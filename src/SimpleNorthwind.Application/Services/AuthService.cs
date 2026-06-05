using SimpleNorthwind.Application.Abstractions.Persistence;
using SimpleNorthwind.Application.Abstractions.Security;
using SimpleNorthwind.Application.Abstractions.Services;
using SimpleNorthwind.Application.Auth;
using SimpleNorthwind.Domain.Common;

namespace SimpleNorthwind.Application.Services;

public sealed class AuthService(
    IEmployeeRepository employees,
    IPasswordHashing passwordHashing,
    IJwtTokenService jwtTokenService) : IAuthService
{
    // 失敗訊息一致：帳號不存在 / 離職 / 密碼錯誤皆回同一個 401，不洩漏帳號是否存在。
    private static readonly Error InvalidCredentials =
        Error.Unauthorized("auth.invalid_credentials", "員工編號或密碼錯誤。");

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var employee = await employees.GetByIdAsync(request.EmployeeId, ct).ConfigureAwait(false);
        if (employee is null || employee.IsResigned)
            return InvalidCredentials;

        if (!passwordHashing.Verify(employee, employee.Password, request.Password))
            return InvalidCredentials;

        var (token, expiresAtUtc) = jwtTokenService.CreateToken(employee);
        return new LoginResponse(token, expiresAtUtc);
    }

    public async Task<Result<LoginResponse>> LoginByNameAsync(LoginByNameRequest request, CancellationToken ct = default)
    {
        var matches = await employees.FindByNameAsync(request.FirstName, request.LastName, ct).ConfigureAwait(false);

        // case-sensitive：DB 可能為 CI collation，故以序數比對挑出大小寫完全相符者；0 或 >1 → 一致 401。
        var exact = matches
            .Where(e => string.Equals(e.FirstName, request.FirstName, StringComparison.Ordinal)
                     && string.Equals(e.LastName, request.LastName, StringComparison.Ordinal))
            .ToList();
        if (exact.Count != 1)
            return InvalidCredentials;

        var employee = exact[0];
        if (employee.IsResigned)
            return InvalidCredentials;
        if (!passwordHashing.Verify(employee, employee.Password, request.Password))
            return InvalidCredentials;

        var (token, expiresAtUtc) = jwtTokenService.CreateToken(employee);
        return new LoginResponse(token, expiresAtUtc);
    }
}
