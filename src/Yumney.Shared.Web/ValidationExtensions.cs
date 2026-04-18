using System.Diagnostics;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;

namespace SmartSolutionsLab.Yumney.Shared.Web;

public static class ValidationExtensions
{
	public static bool HasFailed(this ValidationResult result) => !result.IsValid;

	public static IResult ToValidationProblem(this ValidationResult result)
	{
		LogValidationFailure(result);

		return Results.ValidationProblem(
			result.ToDictionary(),
			statusCode: StatusCodes.Status422UnprocessableEntity,
			extensions: BuildTraceExtensions());
	}

	private static void LogValidationFailure(ValidationResult result)
	{
		var failures = result.Errors;
		if (failures.Count == 0) return;

		var fields = string.Join(", ", failures.Select(e => e.PropertyName).Distinct());
		var activity = Activity.Current;
		activity?.SetTag("validation.failed_fields", fields);
		activity?.SetTag("validation.error_count", failures.Count);
	}

	private static Dictionary<string, object?> BuildTraceExtensions()
	{
		var extensions = new Dictionary<string, object?>();

		var traceId = Activity.Current?.TraceId.ToString();
		if (traceId is not null)
		{
			extensions["traceId"] = traceId;
		}

		return extensions;
	}
}
