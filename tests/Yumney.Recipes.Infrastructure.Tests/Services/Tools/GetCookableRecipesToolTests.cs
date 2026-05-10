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

public class GetCookableRecipesToolTests
{
	private readonly IQueryHandler<GetCookableRecipesQuery, Result<PagedResult<CookableRecipeDto>>> handler =
		Substitute.For<IQueryHandler<GetCookableRecipesQuery, Result<PagedResult<CookableRecipeDto>>>>();

	private readonly ChatToolContext context = new();

	[Fact]
	public async Task GetCookableAsync_FullMatch_AppendsReadyToCookReason()
	{
		var id = Guid.NewGuid();
		handler.HandleAsync(Arg.Any<GetCookableRecipesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result<PagedResult<CookableRecipeDto>>.Success(BuildPage(
				BuildCookable(id, "Risotto", CookableRecipeMatchTier.Full, []))));

		var tool = new GetCookableRecipesTool(handler, context);

		var hits = await tool.GetCookableAsync();

		hits.Should().ContainSingle();
		hits[0].Tier.Should().Be("Full");
		context.Matches.Should().ContainSingle();
		context.Matches[0].Reason.Should().Be("Ready to cook");
		context.ProposeStartCookMode.Should().BeTrue();
	}

	[Fact]
	public async Task GetCookableAsync_NearMatch_AppendsMissingIngredientsReason()
	{
		var id = Guid.NewGuid();
		handler.HandleAsync(Arg.Any<GetCookableRecipesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result<PagedResult<CookableRecipeDto>>.Success(BuildPage(
				BuildCookable(id, "Carbonara", CookableRecipeMatchTier.Near, ["guanciale", "pecorino"]))));

		var tool = new GetCookableRecipesTool(handler, context);

		await tool.GetCookableAsync();

		context.Matches[0].Reason.Should().Be("Missing: guanciale, pecorino");
	}

	[Fact]
	public async Task GetCookableAsync_EmptyResult_DoesNotMarkCookable()
	{
		handler.HandleAsync(Arg.Any<GetCookableRecipesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result<PagedResult<CookableRecipeDto>>.Success(BuildPage()));

		var tool = new GetCookableRecipesTool(handler, context);

		await tool.GetCookableAsync();

		context.Matches.Should().BeEmpty();
		context.ProposeStartCookMode.Should().BeFalse();
	}

	[Fact]
	public async Task GetCookableAsync_FullMatchOnlyParam_ForwardedToHandler()
	{
		handler.HandleAsync(Arg.Any<GetCookableRecipesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result<PagedResult<CookableRecipeDto>>.Success(BuildPage()));

		var tool = new GetCookableRecipesTool(handler, context);

		await tool.GetCookableAsync(fullMatchOnly: true);

		await handler.Received(1).HandleAsync(
			Arg.Is<GetCookableRecipesQuery>(query => query.FullMatchOnly == true),
			Arg.Any<CancellationToken>());
	}

	private static CookableRecipeDto BuildCookable(
		Guid identifier,
		string title,
		CookableRecipeMatchTier tier,
		IReadOnlyList<string> missing) =>
		new(
			identifier,
			title,
			ImageUrl: null,
			Servings: 4,
			PrepTimeMinutes: 10,
			CookTimeMinutes: 20,
			Difficulty: null,
			IngredientCount: 6,
			Tier: tier,
			MissingIngredients: missing);

	private static PagedResult<CookableRecipeDto> BuildPage(params CookableRecipeDto[] items) =>
		new(items, TotalCount: items.Length, Page: 1, PageSize: 5);
}
