namespace SmartSolutionsLab.Yumney.Shared.Common;

public sealed class Result<T> : Result
{
    private readonly T? value;

    public T Value => IsSuccess
        ? value!
        : throw new InvalidOperationException("Cannot access value of a failed result.");

    internal Result(T? value, bool isSuccess, ApiError? error)
        : base(isSuccess, error)
    {
        this.value = value;
    }

    public static Result<T> Success(T value) => new(value, true, null);

    public static new Result<T> Failure(ApiError error) => new(default, false, error);
}
