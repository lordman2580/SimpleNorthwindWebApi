using FluentValidation;
using SimpleNorthwind.Application.Auth;

namespace SimpleNorthwind.Application.Validators;

public sealed class LoginByNameRequestValidator : AbstractValidator<LoginByNameRequest>
{
    public LoginByNameRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty();
        RuleFor(x => x.LastName).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}
