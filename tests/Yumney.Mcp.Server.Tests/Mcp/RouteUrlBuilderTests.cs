using System.Text.Json;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Mcp.Server.Mcp;
using Xunit;

namespace SmartSolutionsLab.Yumney.Mcp.Server.Tests.Mcp;

public class RouteUrlBuilderTests
{
	[Fact]
	public void Build_GetWithoutPlaceholders_NoArgs_ReturnsBareRoute()
	{
		var built = RouteUrlBuilder.Build("GET", "/api/v1/recipes/", arguments: null);

		built.Url.Should().Be("/api/v1/recipes/");
		built.Body.Should().BeNull();
		built.MissingPlaceholders.Should().BeEmpty();
	}

	[Fact]
	public void Build_GetWithExtraArgs_AppendsQueryString()
	{
		var built = RouteUrlBuilder.Build("GET", "/api/v1/recipes/", BuildArgs(("search", "chicken"), ("page", 2)));

		built.Url.Should().Contain("search=chicken").And.Contain("page=2");
		built.Url.Should().StartWith("/api/v1/recipes/?");
		built.Body.Should().BeNull();
	}

	[Fact]
	public void Build_GetWithPlaceholder_SubstitutesAndOmitsFromQuery()
	{
		var built = RouteUrlBuilder.Build("GET", "/api/v1/recipes/{identifier:guid}", BuildArgs(("identifier", "abc-123")));

		built.Url.Should().Be("/api/v1/recipes/abc-123");
		built.Body.Should().BeNull();
	}

	[Fact]
	public void Build_PostWithExtraArgs_PutsArgsInJsonBody()
	{
		var built = RouteUrlBuilder.Build("POST", "/api/v1/shopping-lists/from-recipes", BuildArgs(("title", "This week"), ("recipes", "[]")));

		built.Url.Should().Be("/api/v1/shopping-lists/from-recipes");
		built.Body.Should().NotBeNull();
		built.Body!.Should().Contain("\"title\"");
		built.Body.Should().Contain("\"recipes\"");
	}

	[Fact]
	public void Build_PostWithPlaceholderAndBody_SubstitutesPathArgsAndKeepsBodyArgs()
	{
		var built = RouteUrlBuilder.Build(
			"POST",
			"/api/v1/meal-plans/{year:int}/w/{weekNumber:int}/slots",
			BuildArgs(("year", 2026), ("weekNumber", 19), ("day", "Wednesday"), ("recipeIdentifier", "abc-123")));

		built.Url.Should().Be("/api/v1/meal-plans/2026/w/19/slots");
		built.Body.Should().Contain("\"day\"");
		built.Body.Should().Contain("\"recipeIdentifier\"");
		built.Body.Should().NotContain("\"year\"");
		built.Body.Should().NotContain("\"weekNumber\"");
	}

	[Fact]
	public void Build_MissingRequiredPlaceholder_ReportsIt()
	{
		var built = RouteUrlBuilder.Build("GET", "/api/v1/recipes/{identifier:guid}", arguments: null);

		built.MissingPlaceholders.Should().BeEquivalentTo(["identifier"]);
	}

	[Fact]
	public void Build_PutMethod_UsesBody()
	{
		var built = RouteUrlBuilder.Build(
			"PUT",
			"/api/v1/meal-plans/{year:int}/w/{weekNumber:int}/slots/confirm",
			BuildArgs(("year", 2026), ("weekNumber", 19), ("day", "Wednesday"), ("state", "Cooked")));

		built.Url.Should().Be("/api/v1/meal-plans/2026/w/19/slots/confirm");
		built.Body.Should().NotBeNull();
		built.Body!.Should().Contain("\"day\"");
		built.Body.Should().Contain("\"state\"");
	}

	[Fact]
	public void Build_UrlEscapesPlaceholderValuesAndQueryArgs()
	{
		var built = RouteUrlBuilder.Build("GET", "/api/v1/recipes/{identifier}", BuildArgs(("identifier", "with space"), ("search", "chicken & rice")));

		built.Url.Should().StartWith("/api/v1/recipes/with%20space?");
		built.Url.Should().Contain("search=chicken%20%26%20rice");
	}

	private static Dictionary<string, JsonElement> BuildArgs(params (string Key, object Value)[] pairs)
	{
		var dict = new Dictionary<string, JsonElement>();
		foreach (var (key, value) in pairs)
		{
			var json = value switch
			{
				int number => JsonSerializer.SerializeToElement(number),
				string text => JsonSerializer.SerializeToElement(text),
				_ => JsonSerializer.SerializeToElement(value),
			};
			dict[key] = json;
		}

		return dict;
	}
}
