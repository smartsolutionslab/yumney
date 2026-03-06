namespace Yumney.Shared.Common;

public class Result
{
    public bool IsSuccess { get; }

    public string? Error { get; }

    public bool IsFailure => !IsSuccess;

    protected Result(bool isSuccess, string? error)
    {
        if (isSuccess && error is not null)
        {
            throw new InvalidOperationException("A successful result cannot have an error.");
        }

        if (!isSuccess && string.IsNullOrWhiteSpace(error))
        {
            throw new InvalidOperationException("A failed result must have an error.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);

    public static Result Failure(string error) => new(false, error);

    public static Result<T> Success<T>(T value) => new(value, true, null);

    public static Result<T> Failure<T>(string error) => new(default, false, error);
}

public class Result<T> : Result
{
    private readonly T? value;

    public T Value => IsSuccess
        ? value!
        : throw new InvalidOperationException("Cannot access value of a failed result.");

    internal Result(T? value, bool isSuccess, string? error)
        : base(isSuccess, error)
    {
        this.value = value;
    }

    public static Result<T> Success(T value) => new(value, true, null);

    public static new Result<T> Failure(string error) => new(default, false, error);
}
