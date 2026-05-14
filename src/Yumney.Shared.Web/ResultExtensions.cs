using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.Shared.Web;

public static class ResultExtensions
{
	public static IResult ToOk<T>(this Result<T> result)
	{
		return result.IsFailure
			? ToProblem(result.Error!)
			: Results.Ok(result.Value);
	}

	public static IResult ToCreated<T>(this Result<T> result, string uri)
	{
		return result.IsFailure
			? ToProblem(result.Error!)
			: Results.Created(uri, result.Value);
	}

	public static IResult ToNoContent(this Result result)
	{
		return result.IsFailure
			? ToProblem(result.Error!)
			: Results.NoContent();
	}

	private static IResult ToProblem(ApiError error)
	{
		return Results.Problem(
			detail: error.Message,
			statusCode: error.HttpStatusCode,
			extensions: BuildProblemExtensions(error));
	}

	// Surface the ApiError.Code on the problem-details payload so clients can
	// programmatically distinguish errors within a single 4xx status (e.g.
	// RECIPE_ALREADY_IMPORTED vs RECIPE_ACCESS_DENIED both return 4xx but a
	// human-friendly retry/UX needs to tell them apart). Keeps the trace id
	// alongside for log correlation.
	private static Dictionary<string, object?> BuildProblemExtensions(ApiError error)
	{
		Dictionary<string, object?> extensions = new()
		{
			["code"] = error.Code,
		};

		var traceId = Activity.Current?.TraceId.ToString();
		if (traceId is not null)
		{
			extensions["traceId"] = traceId;
		}

		return extensions;
	}
}
