using FluentAssertions;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.ReadModel;
using Xunit;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Tests.Persistence.ReadModel;

public class MealPlanReadModelMappingExtensionsTests
{
	[Fact]
	public void ToDto_RecipeSlot_PassesThroughEveryField()
	{
		var recipeId = Guid.NewGuid();
		var row = ReadItem(
			day: nameof(DayOfWeek.Tuesday),
			mealType: nameof(MealType.Lunch),
			contentType: nameof(SlotContentType.Recipe),
			state: nameof(MealState.Planned),
			recipeIdentifier: recipeId,
			recipeTitle: "Risotto",
			servings: 3);

		var dto = row.ToDto();

		dto.Day.Should().Be(nameof(DayOfWeek.Tuesday));
		dto.MealType.Should().Be(nameof(MealType.Lunch));
		dto.ContentType.Should().Be(nameof(SlotContentType.Recipe));
		dto.State.Should().Be(nameof(MealState.Planned));
		dto.RecipeIdentifier.Should().Be(recipeId);
		dto.RecipeTitle.Should().Be("Risotto");
		dto.Servings.Should().Be(3);
		dto.IsEmpty.Should().BeFalse();
	}

	[Fact]
	public void ToDto_EmptySlot_FlagsIsEmpty()
	{
		var row = ReadItem(contentType: nameof(SlotContentType.Empty));

		row.ToDto().IsEmpty.Should().BeTrue();
	}

	[Fact]
	public void ToDto_FreetextSlot_DoesNotFlagAsEmpty()
	{
		var row = ReadItem(
			contentType: nameof(SlotContentType.Freetext),
			freetextLabel: "Eat out");

		var dto = row.ToDto();

		dto.IsEmpty.Should().BeFalse();
		dto.FreetextLabel.Should().Be("Eat out");
	}

	[Fact]
	public void ToDtos_OrdersByDayThenMealType()
	{
		// Intentionally seeded out of order — Sunday Lunch, Monday Dinner, Monday Breakfast.
		var rows = new[]
		{
			ReadItem(day: nameof(DayOfWeek.Sunday), mealType: nameof(MealType.Lunch)),
			ReadItem(day: nameof(DayOfWeek.Monday), mealType: nameof(MealType.Dinner)),
			ReadItem(day: nameof(DayOfWeek.Monday), mealType: nameof(MealType.Breakfast)),
		};

		var dtos = rows.ToDtos();

		dtos.Should().HaveCount(3);

		// DayOfWeek enum order: Sunday = 0, Monday = 1.
		// MealType enum order: Dinner = 0, Breakfast = 1, Lunch = 2.
		dtos[0].Day.Should().Be(nameof(DayOfWeek.Sunday));
		dtos[1].Day.Should().Be(nameof(DayOfWeek.Monday));
		dtos[1].MealType.Should().Be(nameof(MealType.Dinner));
		dtos[2].Day.Should().Be(nameof(DayOfWeek.Monday));
		dtos[2].MealType.Should().Be(nameof(MealType.Breakfast));
	}

	[Fact]
	public void ToPlannedRecipeDto_UsesRecipeFields()
	{
		var recipeId = Guid.NewGuid();
		var row = ReadItem(
			day: nameof(DayOfWeek.Friday),
			mealType: nameof(MealType.Dinner),
			recipeIdentifier: recipeId,
			recipeTitle: "Pasta",
			servings: 4);

		var dto = row.ToPlannedRecipeDto();

		dto.RecipeIdentifier.Should().Be(recipeId);
		dto.RecipeTitle.Should().Be("Pasta");
		dto.Servings.Should().Be(4);
		dto.Day.Should().Be(nameof(DayOfWeek.Friday));
		dto.MealType.Should().Be(nameof(MealType.Dinner));
	}

	[Fact]
	public void ToPlannedRecipeDto_NullTitle_FallsBackToEmptyString()
	{
		var row = ReadItem(recipeIdentifier: Guid.NewGuid(), recipeTitle: null);

		row.ToPlannedRecipeDto().RecipeTitle.Should().Be(string.Empty);
	}

	[Fact]
	public void ToHistoryEntryDto_PreservesNullableRecipeIdentifier()
	{
		var row = ReadItem(recipeIdentifier: null, recipeTitle: null);

		var dto = row.ToHistoryEntryDto();

		dto.RecipeIdentifier.Should().BeNull();
		dto.RecipeTitle.Should().Be(string.Empty);
	}

	[Fact]
	public void ToHistoryEntryDtos_PreservesInputOrder()
	{
		var firstId = Guid.NewGuid();
		var secondId = Guid.NewGuid();
		var rows = new[]
		{
			ReadItem(week: "2026-W18", recipeIdentifier: firstId, recipeTitle: "A"),
			ReadItem(week: "2026-W19", recipeIdentifier: secondId, recipeTitle: "B"),
		};

		var dtos = rows.ToHistoryEntryDtos();

		dtos.Should().HaveCount(2);
		dtos[0].Week.Should().Be("2026-W18");
		dtos[0].RecipeIdentifier.Should().Be(firstId);
		dtos[1].Week.Should().Be("2026-W19");
		dtos[1].RecipeIdentifier.Should().Be(secondId);
	}

	private static MealPlanSlotReadItem ReadItem(
		string day = nameof(DayOfWeek.Monday),
		string mealType = nameof(MealType.Dinner),
		string contentType = nameof(SlotContentType.Recipe),
		string state = nameof(MealState.Planned),
		Guid? recipeIdentifier = null,
		string? recipeTitle = null,
		int servings = 4,
		string? freetextLabel = null,
		string? leftoverLabel = null,
		string week = "2026-W18") =>
		new()
		{
			Id = Guid.NewGuid(),
			OwnerId = "owner-test",
			Week = week,
			Day = day,
			MealType = mealType,
			ContentType = contentType,
			State = state,
			RecipeIdentifier = recipeIdentifier,
			RecipeTitle = recipeTitle,
			Servings = servings,
			FreetextLabel = freetextLabel,
			LeftoverLabel = leftoverLabel,
			LastUpdated = DateTime.UtcNow,
		};
}
