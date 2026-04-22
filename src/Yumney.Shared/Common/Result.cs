using System;

namespace SmartSolutionsLab.Yumney.Shared.Common;

public class Result
{
	public bool IsSuccess { get; }

	public ApiError? Error { get; }

	public bool IsFailure => !IsSuccess;

	protected Result(bool isSuccess, ApiError? error)
	{
		if (isSuccess && error is not null) throw new InvalidOperationException("A successful result cannot have an error.");
		if (!isSuccess && error is null) throw new InvalidOperationException("A failed result must have an error.");

		IsSuccess = isSuccess;
		Error = error;
	}

	public static Result Success() => new(true, null);

	public static Result Failure(ApiError error) => new(false, error);

	public static Result<T> Success<T>(T value) => new(value, true, null);

	public static Result<T> Failure<T>(ApiError error) => new(default, false, error);
}
