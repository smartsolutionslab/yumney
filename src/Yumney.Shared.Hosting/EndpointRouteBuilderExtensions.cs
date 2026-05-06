using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace SmartSolutionsLab.Yumney.Shared.Hosting;

public static class EndpointRouteBuilderExtensions
{
	public static RouteGroupBuilder MapApiV1(this WebApplication app)
		=> app
			.MapGroup("/api/v1")
			.RequireAuthorization();
}
