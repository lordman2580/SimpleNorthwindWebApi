using FluentValidation;
using SimpleNorthwind.Application.Auth;

namespace SimpleNorthwind.Application.Validators;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0);
        RuleFor(x => x.Password).NotEmpty();
    }
}
