using System.Globalization;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SimpleNorthwind.Application.Abstractions.Security;
using SimpleNorthwind.Domain.Entities;
using SimpleNorthwind.Infrastructure.Options;

namespace SimpleNorthwind.Infrastructure.Security;

internal sealed class JwtTokenService(IOptions<JwtOptions> options) : IJwtTokenService
{
    public (string Token, DateTime ExpiresAtUtc) CreateToken(Employee employee)
    {
        var jwt = options.Value;
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(jwt.ExpiresMinutes);
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            Expires = expiresAtUtc,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, employee.EmployeeId.ToString(CultureInfo.InvariantCulture)),
                new Claim("name", $"{employee.FirstName} {employee.LastName}"),
                new Claim("title", employee.Title ?? string.Empty)
            ]),
            SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return (token, expiresAtUtc);
    }
}
