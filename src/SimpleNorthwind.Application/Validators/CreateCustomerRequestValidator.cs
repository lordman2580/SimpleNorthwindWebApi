using FluentValidation;
using SimpleNorthwind.Application.Customers;

namespace SimpleNorthwind.Application.Validators;

public sealed class CreateCustomerRequestValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerRequestValidator()
    {
        RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.ContactNumber).MaximumLength(50);
        RuleFor(x => x.ContactTitle).MaximumLength(50);
    }
}
