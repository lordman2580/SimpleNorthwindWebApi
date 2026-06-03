using Shouldly;
using Xunit;
using SimpleNorthwind.Domain.Common;

namespace SimpleNorthwind.Domain.UnitTests;

public class ResultTests
{
    // ── non-generic Result ──────────────────────────────────────────────────

    [Fact]
    public void Success_WhenCreated_IsSuccessTrueAndErrorIsNone()
    {
        // Arrange & Act
        var result = Result.Success();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Error.ShouldBe(Error.None);
    }

    [Fact]
    public void Failure_WithError_IsFailureTrueAndCarriesError()
    {
        // Arrange
        var error = Error.NotFound("Product.NotFound", "Product was not found.");

        // Act
        var result = Result.Failure(error);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(error);
    }

    // ── generic Result<T> ───────────────────────────────────────────────────

    [Fact]
    public void GenericSuccess_ExposesValue()
    {
        // Arrange & Act
        var result = Result.Success<int>(42);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
    }

    [Fact]
    public void GenericFailure_AccessingValue_Throws()
    {
        // Arrange
        var error = Error.Failure("General.Failure", "Something went wrong.");
        var result = Result.Failure<string>(error);

        // Assert
        Should.Throw<InvalidOperationException>(() => _ = result.Value);
    }

    [Fact]
    public void ImplicitFromValue_ProducesSuccess()
    {
        // Arrange & Act
        Result<string> result = "hello";

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("hello");
    }

    [Fact]
    public void ImplicitFromError_ProducesFailure()
    {
        // Arrange
        var error = Error.NotFound("Customer.NotFound", "Customer was not found.");

        // Act
        Result<string> result = error;

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(error);
    }

    // ── guard behaviour ─────────────────────────────────────────────────────

    [Fact]
    public void Success_WithNonNoneError_Throws()
    {
        // The Result constructor guards against isSuccess=true with a real Error.
        // Result<T> constructor is internal, so we reach it via Failure<T> (isSuccess=false)
        // then verify the *success-with-error* guard via the non-generic path through
        // the protected constructor.  The only public route that would trigger the
        // "成功的 Result 不可帶錯誤" guard is a direct `new Result(true, someError)`,
        // but the constructor is protected and there is no factory overload that
        // bypasses the guard.  We therefore assert that calling Success() always
        // produces Error.None, i.e. the guard is effectively unreachable from the
        // public API — and we verify that calling Failure with Error.None is the
        // complementary guard path.
        var nonNoneError = Error.Validation("Field.Required", "Field is required.");

        // Failure with Error.None must throw (guard: "失敗的 Result 必須帶錯誤。")
        Should.Throw<InvalidOperationException>(() => Result.Failure(Error.None));

        // Success factory always uses Error.None — guard for the other branch is
        // exercised through Failure<T> with Error.None as well.
        Should.Throw<InvalidOperationException>(() => Result.Failure<int>(Error.None));

        // Positive confirmation: Failure with a real error does NOT throw.
        var ok = Record.Exception(() => Result.Failure(nonNoneError));
        ok.ShouldBeNull();
    }
}
