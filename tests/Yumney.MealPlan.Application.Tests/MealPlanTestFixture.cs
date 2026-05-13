using NSubstitute;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.TestBuilders.MealPlan;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Tests;

internal static class MealPlanTestFixture
{
	public static readonly OwnerIdentifier TestOwner = OwnerIdentifier.From("user-123");
	public static readonly WeekIdentifier TestWeek = WeekIdentifier.From(2026, 15);

	public static WeeklyPlan CreatePlan() => WeeklyPlanBuilder.A().OwnedBy(TestOwner).ForWeek(TestWeek).Build();

	public static SlotRecipeReference Recipe(string title = "Pasta") =>
		SlotRecipeReference.From(SlotRecipeIdentifier.New(), SlotRecipeTitle.From(title));

	public static SlotRecipeReference Recipe(Guid id, string title) =>
		SlotRecipeReference.From(id, title);

	public static WeeklyPlan SeedPlanWithRecipe(
		FakeMealPlanEventStore eventStore,
		DayOfWeek day = DayOfWeek.Monday,
		string title = "Pasta",
		SlotServings? servings = null)
	{
		var plan = CreatePlan();
		plan.AssignRecipe(day, Recipe(title), servings: servings);
		eventStore.Seed(plan);
		return plan;
	}

	public static ICurrentUser CreateCurrentUser()
	{
		var currentUser = Substitute.For<ICurrentUser>();
		currentUser.UserId.Returns("user-123");
		return currentUser;
	}
}
