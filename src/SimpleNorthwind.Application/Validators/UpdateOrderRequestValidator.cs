using FluentValidation;
using SimpleNorthwind.Application.Orders;

namespace SimpleNorthwind.Application.Validators;

public sealed class UpdateOrderRequestValidator : AbstractValidator<UpdateOrderRequest>
{
    public UpdateOrderRequestValidator()
    {
        RuleFor(x => x.Details).NotEmpty();

        RuleForEach(x => x.Details).ChildRules(detail =>
        {
            detail.RuleFor(d => d.ProductId).GreaterThan(0);
            detail.RuleFor(d => d.OrderQuantities).GreaterThan(0);
            detail.RuleFor(d => d.Discount).InclusiveBetween(0m, 1m);
            detail.RuleFor(d => d.Version).GreaterThan(0);
        });

        RuleFor(x => x.Details)
            .Must(details => details.Select(d => d.ProductId).Distinct().Count() == details.Count)
            .WithMessage("同一訂單不可有重複產品。")
            .When(x => x.Details is { Count: > 0 });
    }
}
