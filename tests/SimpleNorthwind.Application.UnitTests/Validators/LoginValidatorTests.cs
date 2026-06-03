using FluentValidation.TestHelper;
using Shouldly;
using SimpleNorthwind.Application.Auth;
using SimpleNorthwind.Application.Validators;

namespace SimpleNorthwind.Application.UnitTests.Validators;

public sealed class LoginValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    [Fact]
    public void EmptyPassword_ShouldHaveValidationError()
    {
        var request = new LoginRequest(EmployeeId: 1, Password: "");
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void EmployeeIdNotGreaterThanZero_ShouldHaveValidationError(int employeeId)
    {
        var request = new LoginRequest(EmployeeId: employeeId, Password: "secret");
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.EmployeeId);
    }

    [Fact]
    public void ValidRequest_ShouldNotHaveAnyValidationErrors()
    {
        var request = new LoginRequest(EmployeeId: 1, Password: "P@ssw0rd");
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
