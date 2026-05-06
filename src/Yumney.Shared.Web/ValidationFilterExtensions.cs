using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace SmartSolutionsLab.Yumney.Shared.Web;

public static class ValidationFilterExtensions
{
	public static RouteHandlerBuilder WithValidation<TRequest>(this RouteHandlerBuilder builder)
		where TRequest : class
		=> builder.AddEndpointFilter<ValidationEndpointFilter<TRequest>>();
}
