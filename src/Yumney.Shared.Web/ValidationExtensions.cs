using System.Diagnostics;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using HttpResults = Microsoft.AspNetCore.Http.Results;

namespace SmartSolutionsLab.Yumney.Shared.Web;

public static class ValidationExtensions
{
	public static bool HasFailed(this ValidationResult result) => !result.IsValid;

	public static IResult ToValidationProblem(this ValidationResult result)
	{
		LogValidationFailure(result);

		return HttpResults.ValidationProblem(
			result.ToDictionary(),
			statusCode: StatusCodes.Status422UnprocessableEntity,
			extensions: BuildTraceExtensions());
	}

	private static void LogValidationFailure(ValidationResult result)
	{
		var failures = result.Errors;
		if (failures.Count == 0) return;

		var fields = string.Join(", ", failures.Select(failure => failure.PropertyName).Distinct());
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
