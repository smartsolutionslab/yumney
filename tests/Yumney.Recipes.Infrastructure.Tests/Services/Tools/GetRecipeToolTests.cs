using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Queries;
using SmartSolutionsLab.Yumney.Recipes.Extraction.Services;
using SmartSolutionsLab.Yumney.Recipes.Extraction.Services.Tools;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Tests.Services.Tools;

public class GetRecipeToolTests
{
	private readonly IQueryHandler<GetRecipeByIdQuery, Result<RecipeDetailDto>> handler =
		Substitute.For<IQueryHandler<GetRecipeByIdQuery, Result<RecipeDetailDto>>>();

	private readonly ChatToolContext context = new();

	[Fact]
	public async Task GetAsync_HandlerSucceeds_ReturnsDetailAndAppendsContext()
	{
		var id = Guid.NewGuid();
		var detail = BuildDetail(id, "Risotto");
		handler.HandleAsync(Arg.Any<GetRecipeByIdQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result<RecipeDetailDto>.Success(detail));

		var tool = new GetRecipeTool(handler, context);

		var result = await tool.GetAsync(id.ToString());

		result.Should().NotBeNull();
		result!.Title.Should().Be("Risotto");
		context.Matches.Should().ContainSingle();
		context.Matches[0].Identifier.Should().Be(id);
	}

	[Fact]
	public async Task GetAsync_InvalidGuid_ReturnsNullWithoutCallingHandler()
	{
		var tool = new GetRecipeTool(handler, context);

		var result = await tool.GetAsync("not-a-guid");

		result.Should().BeNull();
		context.Matches.Should().BeEmpty();
		await handler.DidNotReceiveWithAnyArgs().HandleAsync(default!, default);
	}

	[Fact]
	public async Task GetAsync_HandlerFails_ReturnsNullWithoutAppendingContext()
	{
		handler.HandleAsync(Arg.Any<GetRecipeByIdQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result<RecipeDetailDto>.Failure(new ApiError("NOT_FOUND", "missing", 404)));

		var tool = new GetRecipeTool(handler, context);

		var result = await tool.GetAsync(Guid.NewGuid().ToString());

		result.Should().BeNull();
		context.Matches.Should().BeEmpty();
	}

	private static RecipeDetailDto BuildDetail(Guid identifier, string title) =>
		new(
			identifier,
			title,
			Description: null,
			Servings: 4,
			PrepTimeMinutes: 10,
			CookTimeMinutes: 20,
			Difficulty: null,
			ImageUrl: null,
			Language: null,
			SourceUrl: null,
			CreatedAt: DateTime.UtcNow,
			Ingredients: [],
			Steps: [],
			Tags: [],
			IsFavorite: false,
			Rating: null,
			Notes: null);
}
