using NSubstitute;
using Shouldly;
using SimpleNorthwind.Application.Abstractions.Persistence;
using SimpleNorthwind.Application.Abstractions.Security;
using SimpleNorthwind.Application.Auth;
using SimpleNorthwind.Application.Services;
using SimpleNorthwind.Domain.Common;
using SimpleNorthwind.Domain.Entities;

namespace SimpleNorthwind.Application.UnitTests;

public sealed class AuthServiceTests
{
    private readonly IEmployeeRepository _employees = Substitute.For<IEmployeeRepository>();
    private readonly IPasswordHashing _passwordHashing = Substitute.For<IPasswordHashing>();
    private readonly IJwtTokenService _jwtTokenService = Substitute.For<IJwtTokenService>();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(_employees, _passwordHashing, _jwtTokenService);
    }

    // ---------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------

    private static Employee MakeEmployee(int id = 1, bool isResigned = false) => new()
    {
        EmployeeId = id,
        FirstName = "Test",
        LastName = "User",
        Password = "hashed-password",
        IsResigned = isResigned
    };

    private static LoginRequest MakeRequest(int employeeId = 1, string password = "correct-password")
        => new(employeeId, password);

    // ---------------------------------------------------------------
    // Happy path
    // ---------------------------------------------------------------

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsSuccessWithToken()
    {
        // Arrange
        var employee = MakeEmployee();
        var request = MakeRequest();
        var expectedToken = "jwt-token-abc";
        var expectedExpiry = new DateTime(2026, 6, 3, 0, 0, 0, DateTimeKind.Utc);

        _employees.GetByIdAsync(request.EmployeeId, Arg.Any<CancellationToken>())
            .Returns(employee);
        _passwordHashing.Verify(employee, employee.Password, request.Password)
            .Returns(true);
        _jwtTokenService.CreateToken(employee)
            .Returns((expectedToken, expectedExpiry));

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Token.ShouldBe(expectedToken);
        result.Value.ExpiresAt.ShouldBe(expectedExpiry);
    }

    // ---------------------------------------------------------------
    // Failure cases
    // ---------------------------------------------------------------

    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsUnauthorized()
    {
        // Arrange
        var employee = MakeEmployee();
        var request = MakeRequest(password: "wrong-password");

        _employees.GetByIdAsync(request.EmployeeId, Arg.Any<CancellationToken>())
            .Returns(employee);
        _passwordHashing.Verify(employee, employee.Password, request.Password)
            .Returns(false);

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task LoginAsync_EmployeeNotFound_ReturnsUnauthorized()
    {
        // Arrange
        var request = MakeRequest();

        _employees.GetByIdAsync(request.EmployeeId, Arg.Any<CancellationToken>())
            .Returns((Employee?)null);

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task LoginAsync_ResignedEmployee_ReturnsUnauthorized()
    {
        // Arrange
        var resigned = MakeEmployee(isResigned: true);
        var request = MakeRequest();

        _employees.GetByIdAsync(request.EmployeeId, Arg.Any<CancellationToken>())
            .Returns(resigned);

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Unauthorized);
    }

    // ---------------------------------------------------------------
    // Consistent error — no account-existence leak
    // ---------------------------------------------------------------

    [Fact]
    public async Task LoginAsync_FailureCases_ReturnSameError()
    {
        // Arrange — not found
        var request = MakeRequest();
        _employees.GetByIdAsync(request.EmployeeId, Arg.Any<CancellationToken>())
            .Returns((Employee?)null);
        var notFoundResult = await _sut.LoginAsync(request);

        // Arrange — wrong password
        var employee = MakeEmployee();
        _employees.GetByIdAsync(request.EmployeeId, Arg.Any<CancellationToken>())
            .Returns(employee);
        _passwordHashing.Verify(employee, employee.Password, request.Password)
            .Returns(false);
        var wrongPasswordResult = await _sut.LoginAsync(request);

        // Arrange — resigned
        var resigned = MakeEmployee(isResigned: true);
        _employees.GetByIdAsync(request.EmployeeId, Arg.Any<CancellationToken>())
            .Returns(resigned);
        var resignedResult = await _sut.LoginAsync(request);

        // Assert — all three failures carry the identical Error (same code)
        notFoundResult.Error.Code.ShouldBe(wrongPasswordResult.Error.Code);
        wrongPasswordResult.Error.Code.ShouldBe(resignedResult.Error.Code);
        notFoundResult.Error.ShouldBe(wrongPasswordResult.Error);
        wrongPasswordResult.Error.ShouldBe(resignedResult.Error);
    }
}
