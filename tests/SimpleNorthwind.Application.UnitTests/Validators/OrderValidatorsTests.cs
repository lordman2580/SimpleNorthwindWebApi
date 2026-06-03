using FluentValidation.TestHelper;
using Shouldly;
using SimpleNorthwind.Application.Orders;
using SimpleNorthwind.Application.Validators;

namespace SimpleNorthwind.Application.UnitTests.Validators;

public sealed class CreateOrderValidatorTests
{
    private readonly CreateOrderRequestValidator _validator = new();

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static CreateOrderDetailRequest ValidDetail(int productId = 1) =>
        new(ProductId: productId, OrderQuantities: 2, Discount: 0m);

    // ── Details collection ───────────────────────────────────────────────────

    [Fact]
    public void EmptyDetails_ShouldHaveValidationError()
    {
        var request = new CreateOrderRequest(
            CustomerId: 1,
            Details: new List<CreateOrderDetailRequest>());

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Details);
    }

    // ── Per-detail rules ─────────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void DetailOrderQuantitiesNotGreaterThanZero_ShouldFailValidation(int qty)
    {
        var request = new CreateOrderRequest(
            CustomerId: 1,
            Details: new List<CreateOrderDetailRequest>
            {
                new(ProductId: 1, OrderQuantities: qty, Discount: 0m)
            });

        _validator.Validate(request).IsValid.ShouldBeFalse();
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    [InlineData(2.0)]
    public void DiscountOutsideZeroToOne_ShouldFailValidation(double discountRaw)
    {
        var discount = (decimal)discountRaw;
        var request = new CreateOrderRequest(
            CustomerId: 1,
            Details: new List<CreateOrderDetailRequest>
            {
                new(ProductId: 1, OrderQuantities: 1, Discount: discount)
            });

        _validator.Validate(request).IsValid.ShouldBeFalse();
    }

    // ── Duplicate ProductId ───────────────────────────────────────────────────

    [Fact]
    public void DuplicateProductIdAcrossDetails_ShouldFailValidation()
    {
        var request = new CreateOrderRequest(
            CustomerId: 1,
            Details: new List<CreateOrderDetailRequest>
            {
                ValidDetail(productId: 5),
                ValidDetail(productId: 5)
            });

        _validator.Validate(request).IsValid.ShouldBeFalse();
    }

    // ── CustomerId ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CustomerIdNotGreaterThanZero_ShouldHaveValidationError(int customerId)
    {
        var request = new CreateOrderRequest(
            CustomerId: customerId,
            Details: new List<CreateOrderDetailRequest> { ValidDetail() });

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.CustomerId);
    }

    // ── Valid ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ValidRequest_ShouldNotHaveAnyValidationErrors()
    {
        var request = new CreateOrderRequest(
            CustomerId: 1,
            Details: new List<CreateOrderDetailRequest>
            {
                new(ProductId: 1, OrderQuantities: 3, Discount: 0.1m),
                new(ProductId: 2, OrderQuantities: 1, Discount: 0m)
            });

        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }
}

public sealed class UpdateOrderValidatorTests
{
    private readonly UpdateOrderRequestValidator _validator = new();

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static UpdateOrderDetailRequest ValidDetail(int productId = 1) =>
        new(ProductId: productId, OrderQuantities: 2, Discount: 0m, Version: 1);

    // ── Details collection ───────────────────────────────────────────────────

    [Fact]
    public void EmptyDetails_ShouldHaveValidationError()
    {
        var request = new UpdateOrderRequest(Details: new List<UpdateOrderDetailRequest>());

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Details);
    }

    // ── Per-detail rules ──────────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void DetailOrderQuantitiesNotGreaterThanZero_ShouldFailValidation(int qty)
    {
        var request = new UpdateOrderRequest(
            Details: new List<UpdateOrderDetailRequest>
            {
                new(ProductId: 1, OrderQuantities: qty, Discount: 0m, Version: 1)
            });

        _validator.Validate(request).IsValid.ShouldBeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void DetailVersionNotGreaterThanZero_ShouldFailValidation(int version)
    {
        var request = new UpdateOrderRequest(
            Details: new List<UpdateOrderDetailRequest>
            {
                new(ProductId: 1, OrderQuantities: 1, Discount: 0m, Version: version)
            });

        _validator.Validate(request).IsValid.ShouldBeFalse();
    }

    // ── Duplicate ProductId ───────────────────────────────────────────────────

    [Fact]
    public void DuplicateProductIdAcrossDetails_ShouldFailValidation()
    {
        var request = new UpdateOrderRequest(
            Details: new List<UpdateOrderDetailRequest>
            {
                ValidDetail(productId: 3),
                ValidDetail(productId: 3)
            });

        _validator.Validate(request).IsValid.ShouldBeFalse();
    }

    // ── Valid ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ValidRequest_ShouldNotHaveAnyValidationErrors()
    {
        var request = new UpdateOrderRequest(
            Details: new List<UpdateOrderDetailRequest>
            {
                new(ProductId: 1, OrderQuantities: 2, Discount: 0.05m, Version: 1),
                new(ProductId: 2, OrderQuantities: 5, Discount: 0m,    Version: 2)
            });

        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
