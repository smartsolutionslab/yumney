using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shared.Web.Middleware;
using HttpResults = Microsoft.AspNetCore.Http.Results;

namespace SmartSolutionsLab.Yumney.Shared.Web;

public static class ResultExtensions
{
	public static IResult ToOk<T>(this Result<T> result)
	{
		return result.IsFailure
			? ToProblem(result.Error!)
			: HttpResults.Ok(result.Value);
	}

	public static IResult ToCreated<T>(this Result<T> result, string uri)
	{
		return result.IsFailure
			? ToProblem(result.Error!)
			: HttpResults.Created(uri, result.Value);
	}

	public static IResult ToNoContent(this Result result)
	{
		return result.IsFailure
			? ToProblem(result.Error!)
			: HttpResults.NoContent();
	}

	private static IResult ToProblem(ApiError error)
	{
		return HttpResults.Problem(
			detail: error.Message,
			statusCode: error.HttpStatusCode,
			extensions: BuildTraceExtensions());
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
