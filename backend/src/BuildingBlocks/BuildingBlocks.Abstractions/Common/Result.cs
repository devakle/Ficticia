namespace BuildingBlocks.Abstractions.Common;

public class Result
{
    public bool IsSuccess { get; }
    public Error? Error { get; }

    protected Result(bool success, Error? error)
    {
        IsSuccess = success;
        Error = error;
    }

    public static Result Ok() => new(true, null);

    public static Result Fail(string code, string message)
        => new(false, new Error(code, message));
}

public sealed class Result<T> : Result
{
    public T? Value { get; }

    private Result(bool success, T? value, Error? error)
        : base(success, error)
    {
        Value = value;
    }

    public static Result<T> Ok(T value)
        => new(true, value, null);

    public static new Result<T> Fail(string code, string message)
        => new(false, default, new Error(code, message));
}
