using System;

namespace SmartSolutionsLab.Yumney.Shared.Outcomes;

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

	public static Result<T> Success(T value)
	{
		// A successful result with a null payload is almost always a bug — the
		// caller meant to return a Failure. Reject it at the construction site
		// rather than handing back a Result whose .Value will NRE downstream.
		ArgumentNullException.ThrowIfNull(value);
		return new(value, true, null);
	}

	public static new Result<T> Failure(ApiError error) => new(default, false, error);

	public static implicit operator Result<T>(T value) => Success(value);

	public static implicit operator Result<T>(ApiError error) => Failure(error);
}
