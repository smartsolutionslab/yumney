using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.Shared.Web;

public static class ResultExtensions
{
	extension(Results)
	{
		/// <summary>
		/// Maps an <see cref="ApiError"/> to an RFC 7807 problem response carrying the
		/// error's status code, message, and <c>code</c> / <c>traceId</c> extensions.
		/// </summary>
		/// <param name="error">The error to surface.</param>
		/// <returns>The problem result.</returns>
		public static IResult Problem(ApiError error)
		{
			return Results.Problem(
				detail: error.Message,
				statusCode: error.HttpStatusCode,
				extensions: BuildProblemExtensions(error));
		}
	}

	public static IResult ToOk<T>(this Result<T> result)
	{
		return result.IsFailure
			? Results.Problem(result.Error!)
			: Results.Ok(result.Value);
	}

	/// <summary>
	/// Maps to 201 Created with a constant location. When the location depends on
	/// the created value, use the <see cref="ToCreated{T}(Result{T}, Func{T, string})"/>
	/// overload — building it from <c>result.Value</c> up front throws on a failed result.
	/// </summary>
	/// <typeparam name="T">The created value type.</typeparam>
	/// <param name="result">The result to map.</param>
	/// <param name="uri">The location of the created resource.</param>
	/// <returns>201 Created on success, otherwise the problem result.</returns>
	public static IResult ToCreated<T>(this Result<T> result, string uri)
	{
		return result.IsFailure
			? Results.Problem(result.Error!)
			: Results.Created(uri, result.Value);
	}

	/// <summary>
	/// Maps to 201 Created, building the location from the created value only on success.
	/// </summary>
	/// <typeparam name="T">The created value type.</typeparam>
	/// <param name="result">The result to map.</param>
	/// <param name="location">Builds the location of the created resource from its value.</param>
	/// <returns>201 Created on success, otherwise the problem result.</returns>
	public static IResult ToCreated<T>(this Result<T> result, Func<T, string> location)
	{
		return result.IsFailure
			? Results.Problem(result.Error!)
			: Results.Created(location(result.Value), result.Value);
	}

	public static IResult ToNoContent(this Result result)
	{
		return result.IsFailure
			? Results.Problem(result.Error!)
			: Results.NoContent();
	}

	/// <summary>
	/// Maps a successful value through <paramref name="onSuccess"/> for responses other
	/// than Ok / Created / NoContent (e.g. <c>Results.Text</c>); failures become the problem result.
	/// </summary>
	/// <typeparam name="T">The value type.</typeparam>
	/// <param name="result">The result to map.</param>
	/// <param name="onSuccess">Builds the success response from the value.</param>
	/// <returns>The success response, otherwise the problem result.</returns>
	public static IResult ToResult<T>(this Result<T> result, Func<T, IResult> onSuccess)
	{
		return result.IsFailure
			? Results.Problem(result.Error!)
			: onSuccess(result.Value);
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
