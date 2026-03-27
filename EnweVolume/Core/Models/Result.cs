using System.Diagnostics.CodeAnalysis;

namespace EnweVolume.Core.Models;

public sealed class Result
{
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess { get; }
    public Error? Error { get; }

    private Result(bool isSuccess, Error? error) =>
        (IsSuccess, Error) = (isSuccess, error);

    public static Result Success() => new(true, default);
    public static Result Failure(Error error) => new(false, error);
}

public sealed class Result<T>
{
    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess { get; }

    public T? Value { get; }
    public Error? Error { get; }

    private Result(bool isSuccess, T? value, Error? error) =>
        (IsSuccess, Value, Error) = (isSuccess, value, error);

    public static Result<T> Success(T value) =>
        new(true, value, default);

    public static Result<T> Failure(Error error) =>
        new(false, default, error);
}
