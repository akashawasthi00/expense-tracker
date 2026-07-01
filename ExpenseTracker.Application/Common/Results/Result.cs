using ExpenseTracker.Application.Common.Errors;

namespace ExpenseTracker.Application.Common.Results;

/// <summary>
/// Outcome of an operation that can fail in an expected way (not-found, forbidden, validation, ...).
/// Services return <see cref="Result"/>/<see cref="Result{T}"/> instead of throwing for control-flow errors;
/// the API translates the <see cref="Error"/> into the right HTTP response.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error? error)
    {
        if (isSuccess && error is not null)
            throw new InvalidOperationException("A successful result cannot carry an error.");
        if (!isSuccess && error is null)
            throw new InvalidOperationException("A failed result must carry an error.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error? Error { get; }

    public static Result Success() => new(true, null);
    public static Result Failure(Error error) => new(false, error);

    public static Result<T> Success<T>(T value) => new(value, true, null);
    public static Result<T> Failure<T>(Error error) => new(default, false, error);

    public static implicit operator Result(Error error) => Failure(error);
}

public sealed class Result<T> : Result
{
    internal Result(T? value, bool isSuccess, Error? error) : base(isSuccess, error) => Value = value;

    public T? Value { get; }

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure<T>(error);
}
