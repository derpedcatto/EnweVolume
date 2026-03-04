using System.Diagnostics.CodeAnalysis;

namespace EnweVolume.Core.Models;

public sealed class Result<T, TError>
{
    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess { get; set; }

    public T? Value { get; }
    public TError? Error { get; }

    private Result(bool isSuccess, T? value, TError? error) =>
        (IsSuccess, Value, Error) = (isSuccess, value, error);

    public static Result<T, TError> Success(T value) =>
        new(true, value, default);

    public static Result<T, TError> Failure(TError error) =>
        new(false, default, error);

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<TError, TResult> onFailure)
        => IsSuccess ? onSuccess(Value) : onFailure(Error);
}
