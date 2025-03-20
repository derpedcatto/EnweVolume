namespace EnweVolume.Core.Models;

public class Result
{
    public bool IsSuccess { get; }
    public string Caption { get; }
    public string Message { get; }

    protected Result(bool isSuccess, string caption, string message)
    {
        IsSuccess = isSuccess;
        Caption = caption;
        Message = message;
    }

    public static Result Success() => new Result(true, string.Empty, string.Empty);
    public static Result Success(string caption, string message) => new Result(true, caption, message);
    public static Result Failure(string caption, string message) => new Result(false, caption, message);
}

public class Result<T> : Result
{
    public T? Value { get; }

    protected Result(bool isSuccess, string caption, string message, T? value)
        : base(isSuccess, caption, message)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new Result<T>(true, string.Empty, string.Empty, value);
    public static new Result<T> Failure(string caption, string message) => new Result<T>(false, caption, message, default);
}