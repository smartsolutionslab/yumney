using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Queries;
using SmartSolutionsLab.Yumney.Recipes.Extraction.Services;
using SmartSolutionsLab.Yumney.Recipes.Extraction.Services.Tools;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shared.Paging;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Tests.Services.Tools;

public class SearchRecipesToolTests
{
	private readonly IQueryHandler<GetRecipesQuery, Result<PagedResult<RecipeListItemDto>>> handler =
		Substitute.For<IQueryHandler<GetRecipesQuery, Result<PagedResult<RecipeListItemDto>>>>();

	private readonly ChatToolContext context = new();

	[Fact]
	public async Task SearchAsync_HandlerReturnsItems_MapsAndAppendsToContext()
	{
		var first = Guid.NewGuid();
		var second = Guid.NewGuid();
		var page = new PagedResult<RecipeListItemDto>(
			[BuildRecipe(first, "Carbonara"), BuildRecipe(second, "Risotto")],
			TotalCount: 2,
			Page: 1,
			PageSize: 10);
		handler.HandleAsync(Arg.Any<GetRecipesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result<PagedResult<RecipeListItemDto>>.Success(page));

		var tool = new SearchRecipesTool(handler, context);

		var hits = await tool.SearchAsync("pasta");

		hits.Should().HaveCount(2);
		hits[0].Title.Should().Be("Carbonara");
		hits[1].Title.Should().Be("Risotto");
		context.Matches.Should().HaveCount(2);
		context.Matches.Select(match => match.Identifier).Should().ContainInOrder(first, second);
	}

	[Fact]
	public async Task SearchAsync_HandlerFails_ReturnsEmptyAndDoesNotTouchContext()
	{
		handler.HandleAsync(Arg.Any<GetRecipesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result<PagedResult<RecipeListItemDto>>.Failure(new ApiError("FAIL", "boom", 500)));

		var tool = new SearchRecipesTool(handler, context);

		var hits = await tool.SearchAsync("pasta");

		hits.Should().BeEmpty();
		context.Matches.Should().BeEmpty();
	}

	[Fact]
	public async Task SearchAsync_PassesQueryAsSearchTerm()
	{
		handler.HandleAsync(Arg.Any<GetRecipesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result<PagedResult<RecipeListItemDto>>.Success(EmptyPage()));

		var tool = new SearchRecipesTool(handler, context);

		await tool.SearchAsync("chicken");

		await handler.Received(1).HandleAsync(
			Arg.Is<GetRecipesQuery>(query => query.Search != null && query.Search.Value == "chicken"),
			Arg.Any<CancellationToken>());
	}

	private static RecipeListItemDto BuildRecipe(Guid identifier, string title) =>
		new(
			identifier,
			title,
			Description: null,
			Servings: 4,
			PrepTimeMinutes: 10,
			CookTimeMinutes: 20,
			Difficulty: null,
			ImageUrl: null,
			CreatedAt: DateTime.UtcNow,
			Tags: [],
			IsFavorite: false,
			Rating: null,
			HasNotes: false);

	private static PagedResult<RecipeListItemDto> EmptyPage() =>
		new([], TotalCount: 0, Page: 1, PageSize: 10);
}
