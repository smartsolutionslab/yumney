using System.Collections.Concurrent;
using System.Reflection;

namespace SmartSolutionsLab.Yumney.Shared.CQRS.Decorators;

#pragma warning disable SA1311 // Static readonly fields should begin with upper-case letter (editorconfig requires camelCase)

/// <summary>
/// Inspects handler return values for the Result pattern without a compile-time
/// dependency on Yumney.Shared. Uses cached reflection to check IsFailure and
/// extract Error.Code / Error.Message from any Result-shaped type.
/// </summary>
internal static class ResultInspector
{
	private static readonly ConcurrentDictionary<Type, ResultAccessor?> cache = new();

	public static bool IsFailure<TResult>(TResult result, out string? errorCode, out string? errorMessage)
	{
		errorCode = null;
		errorMessage = null;

		if (result is null) return false;

		var accessor = cache.GetOrAdd(typeof(TResult), static type =>
		{
			var isFailureProp = type.GetProperty("IsFailure", BindingFlags.Public | BindingFlags.Instance);
			var errorProp = type.GetProperty("Error", BindingFlags.Public | BindingFlags.Instance);

			if (isFailureProp is null || isFailureProp.PropertyType != typeof(bool) || errorProp is null)
				return null;

			var errorType = errorProp.PropertyType;
			var codeProp = errorType.GetProperty("Code", BindingFlags.Public | BindingFlags.Instance);
			var messageProp = errorType.GetProperty("Message", BindingFlags.Public | BindingFlags.Instance);

			return new ResultAccessor(isFailureProp, errorProp, codeProp, messageProp);
		});

		if (accessor is null) return false;

		var isFailure = (bool)accessor.IsFailure.GetValue(result)!;
		if (!isFailure) return false;

		var error = accessor.Error.GetValue(result);
		if (error is null) return true;

		errorCode = accessor.ErrorCode?.GetValue(error) as string;
		errorMessage = accessor.ErrorMessage?.GetValue(error) as string;
		return true;
	}

	private sealed record ResultAccessor(
		PropertyInfo IsFailure,
		PropertyInfo Error,
		PropertyInfo? ErrorCode,
		PropertyInfo? ErrorMessage);
}
