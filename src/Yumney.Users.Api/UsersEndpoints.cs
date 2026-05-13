namespace SmartSolutionsLab.Yumney.Users.Api;

public static class UsersEndpoints
{
	public static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder app)
	{
		app.MapAuthEndpoints();
		app.MapUserActivityEndpoints();
		app.MapProfileEndpoints();
		app.MapStaplesEndpoints();
		app.MapDangerZoneEndpoints();

		return app;
	}
}
