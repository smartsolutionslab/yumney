using FluentAssertions;
using SmartSolutionsLab.Yumney.MealPlan.Application.Interfaces;
using SmartSolutionsLab.Yumney.MealPlan.Application.Queries;
using SmartSolutionsLab.Yumney.MealPlan.Application.Queries.Handlers;
using Xunit;
using static SmartSolutionsLab.Yumney.MealPlan.Application.Tests.MealPlanTestFixture;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Tests.Queries;

public class GetMealAnalyticsQueryHandlerTests
{
	private readonly FakeAnalyticsReadModel readModel = new();
	private readonly FakeRecipeTagsLookup tagsLookup = new();

	[Fact]
	public async Task HandleAsync_NoSlots_ReturnsZeros()
	{
		var handler = BuildHandler();

		var result = await handler.HandleAsync(new GetMealAnalyticsQuery(2026, 5));

		result.IsSuccess.Should().BeTrue();
		var dto = result.Value;
		dto.Period.Should().Be("2026-05");
		dto.TotalCooked.Should().Be(0);
		dto.TotalSkipped.Should().Be(0);
		dto.UniqueRecipes.Should().Be(0);
		dto.MealsPerWeek.Should().Be(0m);
		dto.DiscoveryRate.Should().Be(0);
		dto.TopRecipes.Should().BeEmpty();
		dto.CategoryDistribution.Should().BeEmpty();
	}

	[Fact]
	public async Task HandleAsync_CountsCookedAndSkippedSeparately()
	{
		var lasagna = Guid.NewGuid();
		readModel.SeedCooked(lasagna, "Lasagna", new DateOnly(2026, 5, 10));
		readModel.SeedCooked(lasagna, "Lasagna", new DateOnly(2026, 5, 20));
		readModel.SeedSkipped(new DateOnly(2026, 5, 15));

		var handler = BuildHandler();
		var result = await handler.HandleAsync(new GetMealAnalyticsQuery(2026, 5));

		result.Value.TotalCooked.Should().Be(2);
		result.Value.TotalSkipped.Should().Be(1);
		result.Value.UniqueRecipes.Should().Be(1);
	}

	[Fact]
	public async Task HandleAsync_TopRecipesOrderedByCount()
	{
		var lasagna = Guid.NewGuid();
		var pizza = Guid.NewGuid();
		readModel.SeedCooked(lasagna, "Lasagna", new DateOnly(2026, 5, 5));
		readModel.SeedCooked(lasagna, "Lasagna", new DateOnly(2026, 5, 12));
		readModel.SeedCooked(lasagna, "Lasagna", new DateOnly(2026, 5, 19));
		readModel.SeedCooked(pizza, "Pizza", new DateOnly(2026, 5, 8));

		var handler = BuildHandler();
		var result = await handler.HandleAsync(new GetMealAnalyticsQuery(2026, 5));

		result.Value.TopRecipes.Should().HaveCount(2);
		result.Value.TopRecipes[0].RecipeTitle.Should().Be("Lasagna");
		result.Value.TopRecipes[0].CookCount.Should().Be(3);
		result.Value.TopRecipes[1].RecipeTitle.Should().Be("Pizza");
		result.Value.TopRecipes[1].CookCount.Should().Be(1);
	}

	[Fact]
	public async Task HandleAsync_DiscoveryRateCountsFirstCookInPeriod()
	{
		var newRecipe = Guid.NewGuid();
		var oldRecipe = Guid.NewGuid();
		readModel.SeedCooked(newRecipe, "Pad Thai", new DateOnly(2026, 5, 12));
		readModel.SeedFirstCookDate(newRecipe, new DateOnly(2026, 5, 12));
		readModel.SeedCooked(oldRecipe, "Lasagna", new DateOnly(2026, 5, 20));
		readModel.SeedFirstCookDate(oldRecipe, new DateOnly(2025, 11, 1));

		var handler = BuildHandler();
		var result = await handler.HandleAsync(new GetMealAnalyticsQuery(2026, 5));

		result.Value.DiscoveryRate.Should().Be(1);
	}

	[Fact]
	public async Task HandleAsync_CategoryDistributionFromTags()
	{
		var meatId = Guid.NewGuid();
		var veggieId = Guid.NewGuid();
		var unknownId = Guid.NewGuid();
		readModel.SeedCooked(meatId, "Beef Stew", new DateOnly(2026, 5, 5));
		readModel.SeedCooked(meatId, "Beef Stew", new DateOnly(2026, 5, 12));
		readModel.SeedCooked(veggieId, "Caesar Salad", new DateOnly(2026, 5, 8));
		readModel.SeedCooked(unknownId, "Mystery Bowl", new DateOnly(2026, 5, 15));
		tagsLookup.Seed(meatId, ["beef", "dinner"]);
		tagsLookup.Seed(veggieId, ["vegetarian"]);

		var handler = BuildHandler();
		var result = await handler.HandleAsync(new GetMealAnalyticsQuery(2026, 5));

		var byCategory = result.Value.CategoryDistribution.ToDictionary(share => share.Category);
		byCategory.Should().ContainKey("meat").WhoseValue.Count.Should().Be(2);
		byCategory.Should().ContainKey("veggie").WhoseValue.Count.Should().Be(1);
		byCategory.Should().ContainKey("other").WhoseValue.Count.Should().Be(1);
		byCategory["meat"].Percentage.Should().Be(50m);
	}

	[Fact]
	public async Task HandleAsync_MealsPerWeekRoundedToOneDecimal()
	{
		// 14 meals across 31 days = 14 / (31/7) ≈ 3.16 → rounds to 3.2
		for (var day = 1; day <= 14; day++)
		{
			readModel.SeedCooked(Guid.NewGuid(), $"Recipe {day}", new DateOnly(2026, 5, day));
		}

		var handler = BuildHandler();
		var result = await handler.HandleAsync(new GetMealAnalyticsQuery(2026, 5));

		result.Value.MealsPerWeek.Should().Be(3.2m);
	}

	[Fact]
	public async Task HandleAsync_YearPeriodCoversWholeYear()
	{
		readModel.SeedCooked(Guid.NewGuid(), "January", new DateOnly(2026, 1, 15));
		readModel.SeedCooked(Guid.NewGuid(), "December", new DateOnly(2026, 12, 20));

		var handler = BuildHandler();
		var result = await handler.HandleAsync(new GetMealAnalyticsQuery(2026, null));

		result.Value.Period.Should().Be("2026");
		result.Value.TotalCooked.Should().Be(2);
	}

	private GetMealAnalyticsQueryHandler BuildHandler() => new(readModel, tagsLookup, CreateCurrentUser());
}

internal sealed class FakeAnalyticsReadModel : IMealPlanReadModelRepository
{
	private readonly List<AnalyticsSlotProjection> slots = [];
	private readonly Dictionary<Guid, DateOnly> firstCookDates = [];

	public void SeedCooked(Guid recipeId, string title, DateOnly date) =>
		slots.Add(new AnalyticsSlotProjection(recipeId, title, "Cooked", date));

	public void SeedSkipped(DateOnly date) =>
		slots.Add(new AnalyticsSlotProjection(null, null, "Skipped", date));

	public void SeedFirstCookDate(Guid recipeId, DateOnly date) =>
		firstCookDates[recipeId] = date;

	public Task<IReadOnlyList<AnalyticsSlotProjection>> GetSlotsInPeriodAsync(
		SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan.OwnerIdentifier owner,
		DateOnly periodStart,
		DateOnly periodEndExclusive,
		CancellationToken cancellationToken = default) =>
		Task.FromResult<IReadOnlyList<AnalyticsSlotProjection>>(
			slots.Where(slot => slot.Date >= periodStart && slot.Date < periodEndExclusive).ToList());

	public Task<IReadOnlyDictionary<Guid, DateOnly>> GetFirstCookDatesAsync(
		SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan.OwnerIdentifier owner,
		IReadOnlyList<Guid> recipeIdentifiers,
		CancellationToken cancellationToken = default)
	{
		IReadOnlyDictionary<Guid, DateOnly> filtered = recipeIdentifiers
			.Where(firstCookDates.ContainsKey)
			.ToDictionary(id => id, id => firstCookDates[id]);
		return Task.FromResult(filtered);
	}

	public Task<SmartSolutionsLab.Yumney.MealPlan.Application.DTOs.WeeklyPlanDto> GetByOwnerAndWeekAsync(
		SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan.OwnerIdentifier owner,
		SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan.WeekIdentifier week,
		CancellationToken cancellationToken = default) =>
		throw new NotSupportedException();

	public Task<SmartSolutionsLab.Yumney.MealPlan.Application.DTOs.WeeklyPlannedRecipesDto> GetPlannedRecipesAsync(
		SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan.OwnerIdentifier owner,
		SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan.WeekIdentifier week,
		CancellationToken cancellationToken = default) =>
		throw new NotSupportedException();

	public Task<SmartSolutionsLab.Yumney.Shared.Paging.PagedResult<SmartSolutionsLab.Yumney.MealPlan.Application.DTOs.MealHistoryEntryDto>> SearchCookedHistoryAsync(
		SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan.OwnerIdentifier owner,
		SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan.SearchTerm? term,
		SmartSolutionsLab.Yumney.Shared.Paging.PagingOptions paging,
		CancellationToken cancellationToken = default) =>
		throw new NotSupportedException();
}

internal sealed class FakeRecipeTagsLookup : IRecipeTagsLookup
{
	private readonly Dictionary<Guid, IReadOnlyList<string>> tags = [];

	public void Seed(Guid recipeId, IReadOnlyList<string> recipeTags) => tags[recipeId] = recipeTags;

	public Task<IReadOnlyDictionary<Guid, IReadOnlyList<string>>> GetAllAsync(CancellationToken cancellationToken = default) =>
		Task.FromResult<IReadOnlyDictionary<Guid, IReadOnlyList<string>>>(tags);
}
