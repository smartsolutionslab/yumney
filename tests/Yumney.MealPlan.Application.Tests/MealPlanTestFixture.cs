using NSubstitute;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Tests;

internal static class MealPlanTestFixture
{
	public static readonly OwnerIdentifier TestOwner = OwnerIdentifier.From("user-123");
	public static readonly WeekIdentifier TestWeek = WeekIdentifier.From(2026, 15);

	public static WeeklyPlan CreatePlan() => WeeklyPlan.Create(TestOwner, TestWeek);

	public static SlotRecipeReference Recipe(string title = "Pasta") =>
		SlotRecipeReference.From(Guid.NewGuid(), title);

	public static SlotRecipeReference Recipe(Guid id, string title) =>
		SlotRecipeReference.From(id, title);

	public static WeeklyPlan CreatePlanWithRecipe(
		IWeeklyPlanRepository plans,
		DayOfWeek day = DayOfWeek.Monday,
		string title = "Pasta",
		SlotServings? servings = null)
	{
		var plan = CreatePlan();
		plan.AssignRecipe(day, Recipe(title), servings: servings);

		plans.GetByOwnerAndWeekAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<WeekIdentifier>(), Arg.Any<CancellationToken>())
			.Returns(plan);

		return plan;
	}

	public static ICurrentUser CreateCurrentUser()
	{
		var currentUser = Substitute.For<ICurrentUser>();
		currentUser.UserId.Returns("user-123");
		return currentUser;
	}
}
