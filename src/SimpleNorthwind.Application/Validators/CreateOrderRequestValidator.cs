using FluentValidation;
using SimpleNorthwind.Application.Orders;

namespace SimpleNorthwind.Application.Validators;

public sealed class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0);
        RuleFor(x => x.Details).NotEmpty();

        RuleForEach(x => x.Details).ChildRules(detail =>
        {
            detail.RuleFor(d => d.ProductId).GreaterThan(0);
            detail.RuleFor(d => d.OrderQuantities).GreaterThan(0);
            detail.RuleFor(d => d.Discount).InclusiveBetween(0m, 100m);   // 折扣為百分比 0~100（15=15%）
        });

        RuleFor(x => x.Details)
            .Must(details => details.Select(d => d.ProductId).Distinct().Count() == details.Count)
            .WithMessage("同一訂單不可有重複產品。")
            .When(x => x.Details is { Count: > 0 });
    }
}
