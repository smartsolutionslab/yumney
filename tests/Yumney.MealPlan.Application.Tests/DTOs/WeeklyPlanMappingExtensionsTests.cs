using FluentAssertions;
using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.TestBuilders.MealPlan;
using Xunit;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Tests.DTOs;

public class WeeklyPlanMappingExtensionsTests
{
	[Fact]
	public void WeeklyPlan_ToDto_MapsWeekAndExtendedModeAndSlots()
	{
		var plan = WeeklyPlanBuilder.A()
			.OwnedBy(OwnerIdentifier.From("kc-user-1"))
			.ForWeek(WeekIdentifier.From(2026, 20))
			.Build();

		var dto = plan.ToDto();

		dto.Week.Should().Be("2026-W20");
		dto.IsExtendedMode.Should().BeFalse();
		dto.Slots.Should().NotBeEmpty();
	}

	[Fact]
	public void WeeklyPlan_ToDto_ExtendedMode_SerialisesAllSlots()
	{
		var plan = WeeklyPlanBuilder.A().Build();
		plan.EnableExtendedMode();

		var dto = plan.ToDto();

		dto.IsExtendedMode.Should().BeTrue();

		// Extended mode shows breakfast + lunch + dinner for all 7 days = 21.
		dto.Slots.Should().HaveCount(21);
	}

	[Fact]
	public void MealSlots_ToDtos_OrdersByDayThenMealType()
	{
		var plan = WeeklyPlanBuilder.A().Build();
		plan.EnableExtendedMode();

		var dtos = plan.GetVisibleSlots().ToDtos();

		// FluentAssertions can't translate a tuple-literal selector into an
		// expression tree, so order check is split: ordinal indices into the
		// day enum then the meal-type enum, both monotonically non-decreasing.
		var keys = dtos.Select(slot => ((int)Enum.Parse<DayOfWeek>(slot.Day) * 10) + (int)Enum.Parse<MealType>(slot.MealType)).ToList();
		keys.Should().BeInAscendingOrder();
	}

	[Fact]
	public void MealSlot_ToDto_EmptySlot_HasIsEmptyTrueAndNullRecipeFields()
	{
		var plan = WeeklyPlanBuilder.A().Build();
		var empty = plan.GetVisibleSlots()[0];

		var dto = empty.ToDto();

		dto.IsEmpty.Should().BeTrue();
		dto.ContentType.Should().Be("Empty");
		dto.RecipeIdentifier.Should().BeNull();
		dto.RecipeTitle.Should().BeNull();
		dto.FreetextLabel.Should().BeNull();
		dto.LeftoverLabel.Should().BeNull();
	}

	[Fact]
	public void MealSlot_ToDto_RecipeSlot_StampsRecipeIdentifierAndTitle()
	{
		var plan = WeeklyPlanBuilder.A().Build();
		var recipeId = Guid.NewGuid();
		var recipe = SlotRecipeReference.From(recipeId, "Carbonara");
		plan.AssignRecipe(DayOfWeek.Wednesday, recipe);
		var slot = plan.GetVisibleSlots().Single(slot => slot.Day == DayOfWeek.Wednesday && slot.MealType == MealType.Dinner);

		var dto = slot.ToDto();

		dto.Day.Should().Be("Wednesday");
		dto.MealType.Should().Be("Dinner");
		dto.ContentType.Should().Be("Recipe");
		dto.RecipeIdentifier.Should().Be(recipeId);
		dto.RecipeTitle.Should().Be("Carbonara");
		dto.IsEmpty.Should().BeFalse();
	}

	[Fact]
	public void ToCookedIngredients_FiltersZeroAndNullAmounts()
	{
		IEnumerable<RecipeIngredientLookupResult> ingredients =
		[
			new("Onion", 1m, null, RecipeServings: 4),
			new("Stock", 500m, "ml", RecipeServings: 4),
			new("Salt", null, "g", RecipeServings: 4),
			new("Pepper", 0m, "g", RecipeServings: 4),
		];

		var cooked = ingredients.ToCookedIngredients();

		cooked.Should().HaveCount(2);
		cooked.Select(ingredient => ingredient.Name).Should().BeEquivalentTo(["Onion", "Stock"]);
	}

	[Fact]
	public void ToCookedIngredients_MapsAmountAndUnit()
	{
		IEnumerable<RecipeIngredientLookupResult> ingredients =
		[
			new("Flour", 250m, "g", RecipeServings: 4),
		];

		var cooked = ingredients.ToCookedIngredients().Single();

		cooked.Name.Should().Be("Flour");
		cooked.Quantity.Should().Be(250m);
		cooked.Unit.Should().Be("g");
	}
}
