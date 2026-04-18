using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.MealPlan.Application.Queries;
using SmartSolutionsLab.Yumney.MealPlan.Application.Queries.Handlers;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Common;
using Xunit;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Tests.Queries;

public class GetPlannedRecipesQueryHandlerTests
{
	private readonly IWeeklyPlanRepository plans = Substitute.For<IWeeklyPlanRepository>();
	private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
	private readonly GetPlannedRecipesQueryHandler handler;

	public GetPlannedRecipesQueryHandlerTests()
	{
		currentUser.UserId.Returns("user-123");
		handler = new GetPlannedRecipesQueryHandler(plans, currentUser);
	}

	[Fact]
	public async Task HandleAsync_NoPlan_ReturnsEmptyRecipes()
	{
		plans.FindByOwnerAndWeekAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<WeekIdentifier>(), Arg.Any<CancellationToken>())
			.Returns((WeeklyPlan?)null);

		var result = await handler.HandleAsync(new GetPlannedRecipesQuery(WeekIdentifier.From(2026, 15)));

		result.IsSuccess.Should().BeTrue();
		result.Value.Recipes.Should().BeEmpty();
	}

	[Fact]
	public async Task HandleAsync_OnlyRecipeSlots_Returned()
	{
		var plan = WeeklyPlan.Create(OwnerIdentifier.From("user-123"), WeekIdentifier.From(2026, 15));
		plan.AssignRecipe(DayOfWeek.Monday, SlotRecipeReference.From(Guid.NewGuid(), "Pasta"));
		plan.SetFreetext(DayOfWeek.Tuesday, FreetextLabel.From("Eating out"));
		plan.SetLeftover(DayOfWeek.Wednesday, DayOfWeek.Monday, MealType.Dinner, "Pasta");

		plans.FindByOwnerAndWeekAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<WeekIdentifier>(), Arg.Any<CancellationToken>())
			.Returns(plan);

		var result = await handler.HandleAsync(new GetPlannedRecipesQuery(WeekIdentifier.From(2026, 15)));

		result.Value.Recipes.Should().HaveCount(1);
		result.Value.Recipes[0].RecipeTitle.Should().Be("Pasta");
	}

	[Fact]
	public async Task HandleAsync_MultipleRecipes_AllReturned()
	{
		var plan = WeeklyPlan.Create(OwnerIdentifier.From("user-123"), WeekIdentifier.From(2026, 15));
		plan.AssignRecipe(DayOfWeek.Monday, SlotRecipeReference.From(Guid.NewGuid(), "Pasta"), servings: SlotServings.From(4));
		plan.AssignRecipe(DayOfWeek.Wednesday, SlotRecipeReference.From(Guid.NewGuid(), "Steak"), servings: SlotServings.From(6));
		plan.AssignRecipe(DayOfWeek.Friday, SlotRecipeReference.From(Guid.NewGuid(), "Fish"), servings: SlotServings.From(2));

		plans.FindByOwnerAndWeekAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<WeekIdentifier>(), Arg.Any<CancellationToken>())
			.Returns(plan);

		var result = await handler.HandleAsync(new GetPlannedRecipesQuery(WeekIdentifier.From(2026, 15)));

		result.Value.Recipes.Should().HaveCount(3);
	}

	[Fact]
	public async Task HandleAsync_IncludesServingsPerSlot()
	{
		var plan = WeeklyPlan.Create(OwnerIdentifier.From("user-123"), WeekIdentifier.From(2026, 15));
		plan.AssignRecipe(DayOfWeek.Monday, SlotRecipeReference.From(Guid.NewGuid(), "Pasta"), servings: SlotServings.From(8));

		plans.FindByOwnerAndWeekAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<WeekIdentifier>(), Arg.Any<CancellationToken>())
			.Returns(plan);

		var result = await handler.HandleAsync(new GetPlannedRecipesQuery(WeekIdentifier.From(2026, 15)));

		result.Value.Recipes[0].Servings.Should().Be(8);
	}

	[Fact]
	public async Task HandleAsync_EmptySlots_Excluded()
	{
		var plan = WeeklyPlan.Create(OwnerIdentifier.From("user-123"), WeekIdentifier.From(2026, 15));

		plans.FindByOwnerAndWeekAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<WeekIdentifier>(), Arg.Any<CancellationToken>())
			.Returns(plan);

		var result = await handler.HandleAsync(new GetPlannedRecipesQuery(WeekIdentifier.From(2026, 15)));

		result.Value.Recipes.Should().BeEmpty();
	}
}
