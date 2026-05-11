using FluentAssertions;
using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Application.Interfaces;
using SmartSolutionsLab.Yumney.MealPlan.Application.Queries;
using SmartSolutionsLab.Yumney.MealPlan.Application.Queries.Handlers;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using Xunit;
using static SmartSolutionsLab.Yumney.MealPlan.Application.Tests.MealPlanTestFixture;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Tests.Queries;

public class SuggestWeekPlanQueryHandlerTests
{
	private readonly FakeRecipeCatalogProvider catalog = new();
	private readonly FakeMealPlanReadModelRepository readModel = new();
	private readonly FakeDietaryProfileProvider dietary = new();
	private readonly FakeWeekSuggestionService suggestionService = new();

	[Fact]
	public async Task HandleAsync_EmptyCatalog_ReturnsNoRecipesError()
	{
		var handler = BuildHandler();

		var result = await handler.HandleAsync(new SuggestWeekPlanQuery(TestWeek));

		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(SuggestWeekPlanErrors.NoRecipes);
	}

	[Fact]
	public async Task HandleAsync_CatalogPresent_PassesEverythingToService()
	{
		catalog.Seed(new RecipeCatalogEntry(Guid.NewGuid(), "Lasagna", 15, 45, "Medium", ["pasta"], true, 5));
		catalog.Seed(new RecipeCatalogEntry(Guid.NewGuid(), "Salad", 5, 0, "Easy", ["vegetarian"], false, 4));
		dietary.SetProfile(new DietaryProfileSnapshot("Vegetarian", ["gluten-free"]));
		var historyPlan = CreatePlan();
		historyPlan.AssignRecipe(DayOfWeek.Monday, Recipe("Lasagna"));
		historyPlan.MarkAsCooked(DayOfWeek.Monday);
		readModel.Seed(historyPlan);

		var handler = BuildHandler();
		var result = await handler.HandleAsync(new SuggestWeekPlanQuery(TestWeek));

		result.IsSuccess.Should().BeTrue();
		suggestionService.LastCatalog.Should().HaveCount(2);
		suggestionService.LastDietary.DietaryType.Should().Be("Vegetarian");
		suggestionService.LastDietary.Restrictions.Should().Equal("gluten-free");
		suggestionService.LastHistory.Should().ContainSingle().Which.RecipeTitle.Should().Be("Lasagna");
	}

	[Fact]
	public async Task HandleAsync_ServiceFailure_PropagatesError()
	{
		catalog.Seed(new RecipeCatalogEntry(Guid.NewGuid(), "Lasagna", 15, 45, "Medium", [], false, null));
		suggestionService.ForceFailure(SuggestWeekPlanErrors.SuggestionFailed);

		var handler = BuildHandler();
		var result = await handler.HandleAsync(new SuggestWeekPlanQuery(TestWeek));

		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(SuggestWeekPlanErrors.SuggestionFailed);
	}

	[Fact]
	public async Task HandleAsync_Success_ReturnsServiceEntriesUnderWeekLabel()
	{
		var recipeId = Guid.NewGuid();
		catalog.Seed(new RecipeCatalogEntry(recipeId, "Lasagna", 15, 45, "Medium", [], false, null));
		suggestionService.SetEntries(
		[
			new WeekSuggestionEntryDto("Monday", "Dinner", recipeId, "Lasagna", "Never cooked", null),
		]);

		var handler = BuildHandler();
		var result = await handler.HandleAsync(new SuggestWeekPlanQuery(TestWeek));

		result.Value.Week.Should().Be(TestWeek.ToString());
		result.Value.Entries.Should().ContainSingle().Which.RecipeTitle.Should().Be("Lasagna");
	}

	private SuggestWeekPlanQueryHandler BuildHandler() => new(
		catalog,
		readModel,
		dietary,
		suggestionService,
		CreateCurrentUser());
}

internal sealed class FakeRecipeCatalogProvider : IRecipeCatalogProvider
{
	private readonly List<RecipeCatalogEntry> entries = [];

	public void Seed(RecipeCatalogEntry entry) => entries.Add(entry);

	public Task<IReadOnlyList<RecipeCatalogEntry>> ListAsync(int pageSize, CancellationToken cancellationToken = default) =>
		Task.FromResult<IReadOnlyList<RecipeCatalogEntry>>(entries);
}

internal sealed class FakeDietaryProfileProvider : IDietaryProfileProvider
{
	private DietaryProfileSnapshot profile = DietaryProfileSnapshot.Empty;

	public void SetProfile(DietaryProfileSnapshot snapshot) => profile = snapshot;

	public Task<DietaryProfileSnapshot> GetAsync(CancellationToken cancellationToken = default) =>
		Task.FromResult(profile);
}

internal sealed class FakeWeekSuggestionService : IWeekSuggestionService
{
	private IReadOnlyList<WeekSuggestionEntryDto> entries = [];
	private ApiError? failure;

	public IReadOnlyList<RecipeCatalogEntry> LastCatalog { get; private set; } = [];

	public IReadOnlyList<MealHistoryEntryDto> LastHistory { get; private set; } = [];

	public DietaryProfileSnapshot LastDietary { get; private set; } = DietaryProfileSnapshot.Empty;

	public void SetEntries(IReadOnlyList<WeekSuggestionEntryDto> value) => entries = value;

	public void ForceFailure(ApiError error) => failure = error;

	public Task<Result<IReadOnlyList<WeekSuggestionEntryDto>>> SuggestAsync(
		WeekIdentifier week,
		IReadOnlyList<RecipeCatalogEntry> catalog,
		IReadOnlyList<MealHistoryEntryDto> recentHistory,
		DietaryProfileSnapshot dietary,
		CancellationToken cancellationToken = default)
	{
		LastCatalog = catalog;
		LastHistory = recentHistory;
		LastDietary = dietary;

		if (failure is not null) return Task.FromResult(Result<IReadOnlyList<WeekSuggestionEntryDto>>.Failure(failure));
		return Task.FromResult(Result<IReadOnlyList<WeekSuggestionEntryDto>>.Success(entries));
	}
}
