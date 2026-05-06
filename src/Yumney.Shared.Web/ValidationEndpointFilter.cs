using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace SmartSolutionsLab.Yumney.Shared.Web;

public sealed class ValidationEndpointFilter<TRequest> : IEndpointFilter
	where TRequest : class
{
	public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
	{
		var request = context.Arguments.OfType<TRequest>().FirstOrDefault();
		if (request is null) return await next(context);

		var validator = context.HttpContext.RequestServices.GetService<IValidator<TRequest>>();
		if (validator is null) return await next(context);

		var result = await validator.ValidateAsync(request, context.HttpContext.RequestAborted);
		if (result.HasFailed()) return result.ToValidationProblem();

		return await next(context);
	}
}
