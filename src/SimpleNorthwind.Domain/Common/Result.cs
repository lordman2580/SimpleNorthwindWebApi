namespace SimpleNorthwind.Domain.Common;

/// <summary>業務操作結果。成功 / 失敗（含 Error）。系統錯誤仍 throw。</summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("成功的 Result 不可帶錯誤。");
        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException("失敗的 Result 必須帶錯誤。");

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
    public static Result<T> Success<T>(T value) => new(value, true, Error.None);
    public static Result<T> Failure<T>(Error error) => new(default, false, error);
}

/// <summary>帶值的結果。成功時可取 <see cref="Value"/>。</summary>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    internal Result(T? value, bool isSuccess, Error error) : base(isSuccess, error) => _value = value;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("無法存取失敗 Result 的 Value。");

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure<T>(error);
}
