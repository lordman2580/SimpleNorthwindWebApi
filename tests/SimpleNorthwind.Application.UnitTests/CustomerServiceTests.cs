using NSubstitute;
using Shouldly;
using SimpleNorthwind.Application.Abstractions.Persistence;
using SimpleNorthwind.Application.Customers;
using SimpleNorthwind.Application.Services;
using SimpleNorthwind.Domain.Common;
using SimpleNorthwind.Domain.Entities;

namespace SimpleNorthwind.Application.UnitTests;

public sealed class CustomerServiceTests
{
    private readonly ICustomerRepository _repo = Substitute.For<ICustomerRepository>();
    private readonly CustomerService _sut;

    public CustomerServiceTests()
    {
        _sut = new CustomerService(_repo);
    }

    // -----------------------------------------------------------------------
    // CreateAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_Valid_ReturnsSuccessWithDto()
    {
        // Arrange
        const int newId = 42;
        const string actingUser = "test-user";
        var request = new CreateCustomerRequest("Acme Corp", "0912-345-678", "Manager");

        _repo.InsertAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>())
             .Returns(newId);

        // Act
        var result = await _sut.CreateAsync(request, actingUser);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.CustomerId.ShouldBe(newId);
        result.Value.CreateUser.ShouldBe(actingUser);
    }

    // -----------------------------------------------------------------------
    // GetAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetAsync_Found_ReturnsDto()
    {
        // Arrange
        var customer = BuildCustomer(id: 1);
        _repo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(customer);

        // Act
        var result = await _sut.GetAsync(1);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.CustomerId.ShouldBe(customer.CustomerId);
        result.Value.CompanyName.ShouldBe(customer.CompanyName);
    }

    [Fact]
    public async Task GetAsync_NotFound_ReturnsNotFound()
    {
        // Arrange
        _repo.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Customer?)null);

        // Act
        var result = await _sut.GetAsync(99);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }

    // -----------------------------------------------------------------------
    // UpdateAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_Found_ReturnsSuccess()
    {
        // Arrange
        var customer = BuildCustomer(id: 5);
        var request = new UpdateCustomerRequest("Updated Corp", "0900-000-000", "Director", false, null);

        _repo.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(customer);
        _repo.UpdateAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _sut.UpdateAsync(5, request);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.CompanyName.ShouldBe("Updated Corp");
        result.Value.ContactTitle.ShouldBe("Director");
    }

    [Fact]
    public async Task UpdateAsync_NoChanges_ReturnsValidationNotModifiedAndSkipsWrite()
    {
        // Arrange：請求與現況完全相同
        var customer = BuildCustomer(id: 7);
        _repo.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(customer);
        var request = new UpdateCustomerRequest(
            customer.CompanyName, customer.ContactNumber, customer.ContactTitle,
            customer.IsOutContacted, customer.OutContactedDate);

        // Act
        var result = await _sut.UpdateAsync(7, request);

        // Assert：回 Validation（→ 400），不寫 DB
        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Code.ShouldBe("customer.not_modified");
        await _repo.DidNotReceive().UpdateAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ReturnsNotFound()
    {
        // Arrange
        _repo.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Customer?)null);
        var request = new UpdateCustomerRequest("X", null, null, false, null);

        // Act
        var result = await _sut.UpdateAsync(99, request);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }

    // -----------------------------------------------------------------------
    // DeleteAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task DeleteAsync_Exists_ReturnsSuccess()
    {
        // Arrange
        _repo.DeleteAsync(3, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _sut.DeleteAsync(3);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ReturnsNotFound()
    {
        // Arrange
        _repo.DeleteAsync(99, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _sut.DeleteAsync(99);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }

    // -----------------------------------------------------------------------
    // ListAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ListAsync_ReturnsMappedDtos()
    {
        // Arrange
        var customers = new List<Customer>
        {
            BuildCustomer(id: 1, company: "Alpha"),
            BuildCustomer(id: 2, company: "Beta"),
            BuildCustomer(id: 3, company: "Gamma")
        };
        _repo.ListAsync(Arg.Any<CancellationToken>()).Returns((IReadOnlyList<Customer>)customers);

        // Act
        var dtos = await _sut.ListAsync();

        // Assert
        dtos.Count.ShouldBe(3);
        dtos.Select(d => d.CustomerId).ShouldBe(new[] { 1, 2, 3 });
        dtos.Select(d => d.CompanyName).ShouldBe(new[] { "Alpha", "Beta", "Gamma" });
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static Customer BuildCustomer(int id = 1, string company = "Test Corp") =>
        new()
        {
            CustomerId = id,
            CompanyName = company,
            ContactNumber = "0900-111-222",
            ContactTitle = "CEO",
            CreateDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CreateUser = "seed-user",
            IsOutContacted = false,
            OutContactedDate = null
        };
}
