using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SmartSolutionsLab.Yumney.Shared.Hosting;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Hosting;

public class EndpointRouteBuilderExtensionsTests
{
	[Fact]
	public void MapApiV1_GroupRequiresAuthorization()
	{
		var app = WebApplication.CreateBuilder().Build();

		var group = app.MapApiV1();
		group.MapGet("/probe", () => "ok");

		var endpoint = ((IEndpointRouteBuilder)app).DataSources
			.SelectMany(source => source.Endpoints)
			.OfType<RouteEndpoint>()
			.Single(route => route.RoutePattern.RawText == "/api/v1/probe");

		endpoint.Metadata.GetMetadata<IAuthorizeData>().Should().NotBeNull("MapApiV1 wires .RequireAuthorization() on the group");
	}

	[Fact]
	public void MapApiV1_GroupHasApiV1Prefix()
	{
		var app = WebApplication.CreateBuilder().Build();

		var group = app.MapApiV1();
		group.MapGet("/recipes", () => "ok");

		var endpoint = ((IEndpointRouteBuilder)app).DataSources
			.SelectMany(source => source.Endpoints)
			.OfType<RouteEndpoint>()
			.Single();

		endpoint.RoutePattern.RawText.Should().Be("/api/v1/recipes");
	}
}
