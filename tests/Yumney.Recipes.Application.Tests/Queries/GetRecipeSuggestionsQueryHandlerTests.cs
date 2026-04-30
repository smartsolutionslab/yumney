using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Application.Queries;
using SmartSolutionsLab.Yumney.Recipes.Application.Queries.Handlers;
using SmartSolutionsLab.Yumney.Shared.Common;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Tests.Queries;

public class GetRecipeSuggestionsQueryHandlerTests
{
	private const string OwnerId = "user-123";

	private readonly IIngredientBalanceProvider balanceProvider = Substitute.For<IIngredientBalanceProvider>();
	private readonly IDietaryProfileProvider dietaryProvider = Substitute.For<IDietaryProfileProvider>();
	private readonly IRecipeSuggestionService suggestionService = Substitute.For<IRecipeSuggestionService>();
	private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
	private readonly GetRecipeSuggestionsQueryHandler handler;

	public GetRecipeSuggestionsQueryHandlerTests()
	{
		currentUser.UserId.Returns(OwnerId);
		dietaryProvider.GetAsync(OwnerId, Arg.Any<CancellationToken>())
			.Returns(DietaryProfileSnapshot.Empty);
		handler = new GetRecipeSuggestionsQueryHandler(balanceProvider, dietaryProvider, suggestionService, currentUser);
	}

	[Fact]
	public async Task HandleAsync_NoAvailableIngredients_ReturnsNoIngredientsError()
	{
		ConfigureBalance();

		var result = await handler.HandleAsync(new GetRecipeSuggestionsQuery());

		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(RecipeSuggestionErrors.NoIngredients);
		await suggestionService.DidNotReceiveWithAnyArgs().SuggestAsync(default!, default, default!, default);
	}

	[Fact]
	public async Task HandleAsync_PassesIngredientsAndCountToSuggestionService()
	{
		ConfigureBalance("chicken", "rice");
		ConfigureDietary("vegetarian", ["gluten-free"]);
		suggestionService.SuggestAsync(
			Arg.Any<IReadOnlyCollection<string>>(),
			Arg.Any<string?>(),
			Arg.Any<IReadOnlyCollection<string>>(),
			Arg.Any<int>(),
			Arg.Any<CancellationToken>())
			.Returns(Result<IReadOnlyList<ExtractedRecipeDto>>.Success([]));

		await handler.HandleAsync(new GetRecipeSuggestionsQuery(Count: 3));

		await suggestionService.Received(1).SuggestAsync(
			Arg.Is<IReadOnlyCollection<string>>(c => c.Contains("chicken") && c.Contains("rice")),
			"vegetarian",
			Arg.Is<IReadOnlyCollection<string>>(c => c.Contains("gluten-free")),
			3,
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_CountClampedToTen()
	{
		ConfigureBalance("apple");
		suggestionService.SuggestAsync(
			Arg.Any<IReadOnlyCollection<string>>(),
			Arg.Any<string?>(),
			Arg.Any<IReadOnlyCollection<string>>(),
			Arg.Any<int>(),
			Arg.Any<CancellationToken>())
			.Returns(Result<IReadOnlyList<ExtractedRecipeDto>>.Success([]));

		await handler.HandleAsync(new GetRecipeSuggestionsQuery(Count: 999));

		await suggestionService.Received(1).SuggestAsync(
			Arg.Any<IReadOnlyCollection<string>>(),
			Arg.Any<string?>(),
			Arg.Any<IReadOnlyCollection<string>>(),
			10,
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_CountClampedToOne()
	{
		ConfigureBalance("apple");
		suggestionService.SuggestAsync(
			Arg.Any<IReadOnlyCollection<string>>(),
			Arg.Any<string?>(),
			Arg.Any<IReadOnlyCollection<string>>(),
			Arg.Any<int>(),
			Arg.Any<CancellationToken>())
			.Returns(Result<IReadOnlyList<ExtractedRecipeDto>>.Success([]));

		await handler.HandleAsync(new GetRecipeSuggestionsQuery(Count: 0));

		await suggestionService.Received(1).SuggestAsync(
			Arg.Any<IReadOnlyCollection<string>>(),
			Arg.Any<string?>(),
			Arg.Any<IReadOnlyCollection<string>>(),
			1,
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_NoDietaryPreferences_PassesNullDietaryType()
	{
		ConfigureBalance("apple");
		dietaryProvider.GetAsync(OwnerId, Arg.Any<CancellationToken>())
			.Returns(DietaryProfileSnapshot.Empty);
		suggestionService.SuggestAsync(
			Arg.Any<IReadOnlyCollection<string>>(),
			Arg.Any<string?>(),
			Arg.Any<IReadOnlyCollection<string>>(),
			Arg.Any<int>(),
			Arg.Any<CancellationToken>())
			.Returns(Result<IReadOnlyList<ExtractedRecipeDto>>.Success([]));

		await handler.HandleAsync(new GetRecipeSuggestionsQuery());

		await suggestionService.Received(1).SuggestAsync(
			Arg.Any<IReadOnlyCollection<string>>(),
			(string?)null,
			Arg.Is<IReadOnlyCollection<string>>(c => c.Count == 0),
			Arg.Any<int>(),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_PropagatesSuggestionServiceFailure()
	{
		ConfigureBalance("apple");
		suggestionService.SuggestAsync(
			Arg.Any<IReadOnlyCollection<string>>(),
			Arg.Any<string?>(),
			Arg.Any<IReadOnlyCollection<string>>(),
			Arg.Any<int>(),
			Arg.Any<CancellationToken>())
			.Returns(RecipeSuggestionErrors.SuggestionFailed);

		var result = await handler.HandleAsync(new GetRecipeSuggestionsQuery());

		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(RecipeSuggestionErrors.SuggestionFailed);
	}

	[Fact]
	public async Task HandleAsync_PropagatesSuggestionServiceSuccess()
	{
		ConfigureBalance("apple");
		var suggestions = new List<ExtractedRecipeDto>
		{
			new("Apple Pie", [new ExtractedIngredientDto("Apple", 5, null)], [new ExtractedStepDto(1, "Bake")]),
		};
		suggestionService.SuggestAsync(
			Arg.Any<IReadOnlyCollection<string>>(),
			Arg.Any<string?>(),
			Arg.Any<IReadOnlyCollection<string>>(),
			Arg.Any<int>(),
			Arg.Any<CancellationToken>())
			.Returns(Result<IReadOnlyList<ExtractedRecipeDto>>.Success(suggestions));

		var result = await handler.HandleAsync(new GetRecipeSuggestionsQuery());

		result.IsSuccess.Should().BeTrue();
		result.Value.Should().ContainSingle().Which.Title.Should().Be("Apple Pie");
	}

	[Fact]
	public async Task HandleAsync_QueriesProvidersWithCurrentUser()
	{
		ConfigureBalance("apple");
		suggestionService.SuggestAsync(
			Arg.Any<IReadOnlyCollection<string>>(),
			Arg.Any<string?>(),
			Arg.Any<IReadOnlyCollection<string>>(),
			Arg.Any<int>(),
			Arg.Any<CancellationToken>())
			.Returns(Result<IReadOnlyList<ExtractedRecipeDto>>.Success([]));

		await handler.HandleAsync(new GetRecipeSuggestionsQuery());

		await balanceProvider.Received(1).GetAvailableIngredientsAsync(OwnerId, Arg.Any<CancellationToken>());
		await dietaryProvider.Received(1).GetAsync(OwnerId, Arg.Any<CancellationToken>());
	}

	private void ConfigureBalance(params string[] names)
	{
		var dict = new Dictionary<string, Freshness>(StringComparer.OrdinalIgnoreCase);
		foreach (var name in names) dict[name] = Freshness.Fresh;
		balanceProvider.GetAvailableIngredientsAsync(OwnerId, Arg.Any<CancellationToken>())
			.Returns((IReadOnlyDictionary<string, Freshness>)dict);
	}

	private void ConfigureDietary(string? dietaryType, IReadOnlyList<string> restrictions)
	{
		dietaryProvider.GetAsync(OwnerId, Arg.Any<CancellationToken>())
			.Returns(new DietaryProfileSnapshot(dietaryType, restrictions));
	}
}
