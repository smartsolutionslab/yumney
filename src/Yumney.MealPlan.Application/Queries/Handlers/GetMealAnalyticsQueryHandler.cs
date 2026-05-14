using System.Globalization;
using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Queries.Handlers;

public sealed class GetMealAnalyticsQueryHandler(
	IMealPlanReadModelRepository readModel,
	IRecipeTagsLookup tags,
	ICurrentUser currentUser)
	: IQueryHandler<GetMealAnalyticsQuery, Result<MealAnalyticsDto>>
{
#pragma warning disable SA1303
	private const int topRecipeLimit = 5;
	private const decimal daysPerWeek = 7m;
#pragma warning restore SA1303

	public async Task<Result<MealAnalyticsDto>> HandleAsync(
		GetMealAnalyticsQuery query,
		CancellationToken cancellationToken = default)
	{
		var owner = currentUser.AsOwner();
		var (periodStart, periodEndExclusive) = PeriodBounds(query.Year, query.Month);

		var slots = await readModel.GetSlotsInPeriodAsync(owner, periodStart, periodEndExclusive, cancellationToken);

		List<AnalyticsSlotProjection> cooked = [];
		var skippedCount = 0;
		foreach (var slot in slots)
		{
			if (slot.State == "Cooked") cooked.Add(slot);
			else if (slot.State == "Skipped") skippedCount++;
		}

		var recipeCooks = cooked
			.Where(slot => slot.RecipeIdentifier.HasValue && slot.RecipeTitle is not null)
			.GroupBy(slot => slot.RecipeIdentifier!.Value)
			.Select(group => new RecipeCookCount(group.Key, group.First().RecipeTitle!, group.Count()))
			.ToList();

		var uniqueRecipes = recipeCooks.Count;
		var recipeIds = recipeCooks.Select(row => row.RecipeIdentifier).ToList();

		var firstCookDates = recipeIds.Count == 0
			? new Dictionary<Guid, DateOnly>()
			: (await readModel.GetFirstCookDatesAsync(owner, recipeIds, cancellationToken))
				.ToDictionary(entry => entry.Key, entry => entry.Value);
		var discoveryRate = recipeIds.Count(id =>
			firstCookDates.TryGetValue(id, out var firstDate) && firstDate >= periodStart && firstDate < periodEndExclusive);

		var tagsByRecipe = recipeIds.Count == 0
			? new Dictionary<Guid, IReadOnlyList<string>>()
			: (await tags.GetAllAsync(cancellationToken))
				.Where(entry => recipeIds.Contains(entry.Key))
				.ToDictionary(entry => entry.Key, entry => entry.Value);

		var dto = new MealAnalyticsDto(
			FormatPeriod(query.Year, query.Month),
			cooked.Count,
			skippedCount,
			uniqueRecipes,
			MealsPerWeek(cooked.Count, periodStart, periodEndExclusive),
			discoveryRate,
			recipeCooks
				.OrderByDescending(row => row.Count)
				.ThenBy(row => row.Title, StringComparer.OrdinalIgnoreCase)
				.Take(topRecipeLimit)
				.Select(row => row.ToDto())
				.ToList(),
			BuildCategoryDistribution(cooked, tagsByRecipe));

		return Result<MealAnalyticsDto>.Success(dto);
	}

	private static (DateOnly Start, DateOnly EndExclusive) PeriodBounds(int year, int? month)
	{
		if (month is { } monthValue)
		{
			var start = new DateOnly(year, monthValue, 1);
			var end = start.AddMonths(1);
			return (start, end);
		}

		return (new DateOnly(year, 1, 1), new DateOnly(year + 1, 1, 1));
	}

	private static string FormatPeriod(int year, int? month) =>
		month is { } monthValue
			? string.Create(CultureInfo.InvariantCulture, $"{year:0000}-{monthValue:00}")
			: year.ToString("0000", CultureInfo.InvariantCulture);

	private static decimal MealsPerWeek(int cookedCount, DateOnly start, DateOnly endExclusive)
	{
		if (cookedCount == 0) return 0m;
		var days = (decimal)(endExclusive.DayNumber - start.DayNumber);
		if (days <= 0m) return 0m;
		var weeks = days / daysPerWeek;
		return Math.Round(cookedCount / weeks, 1, MidpointRounding.AwayFromZero);
	}

	private static List<CategoryShareDto> BuildCategoryDistribution(
		List<AnalyticsSlotProjection> cooked,
		Dictionary<Guid, IReadOnlyList<string>> tagsByRecipe)
	{
		if (cooked.Count == 0) return [];

		Dictionary<string, int> totals = new(StringComparer.Ordinal);
		foreach (var slot in cooked)
		{
			var category = slot.RecipeIdentifier is { } id && tagsByRecipe.TryGetValue(id, out var recipeTags)
				? CategorizeTags(recipeTags)
				: "other";
			totals[category] = totals.GetValueOrDefault(category) + 1;
		}

		var total = (decimal)cooked.Count;
		return totals
			.OrderByDescending(entry => entry.Value)
			.ThenBy(entry => entry.Key, StringComparer.Ordinal)
			.Select(entry => BuildCategoryShare(entry.Key, entry.Value, total))
			.ToList();
	}

	private static CategoryShareDto BuildCategoryShare(string category, int count, decimal totalCooked) =>
		new(category, count, Math.Round(count / totalCooked * 100m, 1, MidpointRounding.AwayFromZero));

#pragma warning disable SA1402, SA1649
	private sealed record RecipeCookCount(Guid RecipeIdentifier, string Title, int Count)
	{
		public TopRecipeDto ToDto() => new(this.RecipeIdentifier, this.Title, this.Count);
	}
#pragma warning restore SA1402, SA1649

	private static string CategorizeTags(IReadOnlyList<string> recipeTags)
	{
		// Tag matching is case-insensitive and substring-based to forgive
		// "vegetarian" vs "veggie" vs "vegan" variations user-defined tags
		// pick up. Order matters: vegan beats veggie beats meat/fish.
		var lower = recipeTags.Select(tag => tag.ToLowerInvariant()).ToList();
		if (lower.Any(tag => tag.Contains("vegan", StringComparison.Ordinal))) return "vegan";
		if (lower.Any(tag => tag.Contains("vegetarian", StringComparison.Ordinal) || tag.Contains("veggie", StringComparison.Ordinal))) return "veggie";
		if (lower.Any(tag => tag.Contains("fish", StringComparison.Ordinal) || tag.Contains("seafood", StringComparison.Ordinal))) return "fish";
		if (lower.Any(tag => tag.Contains("meat", StringComparison.Ordinal) || tag.Contains("chicken", StringComparison.Ordinal) || tag.Contains("beef", StringComparison.Ordinal) || tag.Contains("pork", StringComparison.Ordinal))) return "meat";
		return "other";
	}
}
