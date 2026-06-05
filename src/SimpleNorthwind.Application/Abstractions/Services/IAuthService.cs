using SimpleNorthwind.Application.Auth;
using SimpleNorthwind.Domain.Common;

namespace SimpleNorthwind.Application.Abstractions.Services;

public interface IAuthService
{
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);

    /// <summary>以姓名（first + last，**case-sensitive**）+ 密碼登入（UD11）。</summary>
    Task<Result<LoginResponse>> LoginByNameAsync(LoginByNameRequest request, CancellationToken ct = default);
}
