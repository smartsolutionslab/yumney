using Microsoft.AspNetCore.Http;
using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Shared.Web;

public static class ResultExtensions
{
    public static IResult ToOk<T>(this Result<T> result)
    {
        return result.IsFailure
            ? Results.Problem(result.Error!.Message, statusCode: result.Error.HttpStatusCode)
            : Results.Ok(result.Value);
    }

    public static IResult ToCreated<T>(this Result<T> result, string uri)
    {
        return result.IsFailure
            ? Results.Problem(result.Error!.Message, statusCode: result.Error.HttpStatusCode)
            : Results.Created(uri, result.Value);
    }

    public static IResult ToNoContent(this Result result)
    {
        return result.IsFailure
            ? Results.Problem(result.Error!.Message, statusCode: result.Error.HttpStatusCode)
            : Results.NoContent();
    }
}
