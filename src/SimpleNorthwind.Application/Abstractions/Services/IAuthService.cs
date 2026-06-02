using SimpleNorthwind.Application.Auth;
using SimpleNorthwind.Domain.Common;

namespace SimpleNorthwind.Application.Abstractions.Services;

public interface IAuthService
{
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
}
