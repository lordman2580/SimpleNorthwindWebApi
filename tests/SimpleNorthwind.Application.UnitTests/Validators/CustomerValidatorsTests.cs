using FluentValidation.TestHelper;
using Shouldly;
using SimpleNorthwind.Application.Customers;
using SimpleNorthwind.Application.Validators;

namespace SimpleNorthwind.Application.UnitTests.Validators;

public sealed class CreateCustomerValidatorTests
{
    private readonly CreateCustomerRequestValidator _validator = new();

    [Fact]
    public void EmptyCompanyName_ShouldHaveValidationError()
    {
        var request = new CreateCustomerRequest(
            CompanyName: "",
            ContactNumber: null,
            ContactTitle: null);

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.CompanyName);
    }

    [Fact]
    public void CompanyNameOver150Chars_ShouldHaveValidationError()
    {
        var request = new CreateCustomerRequest(
            CompanyName: new string('A', 151),
            ContactNumber: null,
            ContactTitle: null);

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.CompanyName);
    }

    [Fact]
    public void ValidRequest_ShouldNotHaveAnyValidationErrors()
    {
        var request = new CreateCustomerRequest(
            CompanyName: "Northwind Traders",
            ContactNumber: "02-12345678",
            ContactTitle: "Manager");

        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }
}

public sealed class UpdateCustomerValidatorTests
{
    private readonly UpdateCustomerRequestValidator _validator = new();

    [Fact]
    public void EmptyCompanyName_ShouldHaveValidationError()
    {
        var request = new UpdateCustomerRequest(
            CompanyName: "",
            ContactNumber: null,
            ContactTitle: null,
            IsOutContacted: false,
            OutContactedDate: null);

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.CompanyName);
    }

    [Fact]
    public void CompanyNameOver150Chars_ShouldHaveValidationError()
    {
        var request = new UpdateCustomerRequest(
            CompanyName: new string('B', 151),
            ContactNumber: null,
            ContactTitle: null,
            IsOutContacted: false,
            OutContactedDate: null);

        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.CompanyName);
    }

    [Fact]
    public void ValidRequest_ShouldNotHaveAnyValidationErrors()
    {
        var request = new UpdateCustomerRequest(
            CompanyName: "Northwind Traders",
            ContactNumber: "02-12345678",
            ContactTitle: "Director",
            IsOutContacted: false,
            OutContactedDate: null);

        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
