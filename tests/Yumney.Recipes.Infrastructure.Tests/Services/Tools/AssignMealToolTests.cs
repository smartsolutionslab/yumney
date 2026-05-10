using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Extraction.Services;
using SmartSolutionsLab.Yumney.Recipes.Extraction.Services.Tools;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Tests.Services.Tools;

public class AssignMealToolTests
{
	private readonly IMealPlanScheduler scheduler = Substitute.For<IMealPlanScheduler>();
	private readonly ChatToolContext context = new();

	[Fact]
	public async Task AssignAsync_HappyPath_ForwardsRequestAndAppendsContext()
	{
		var recipe = Guid.NewGuid();
		AssignMealRequest? captured = null;
		scheduler.AssignAsync(Arg.Any<AssignMealRequest>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				captured = callInfo.ArgAt<AssignMealRequest>(0);
				return true;
			});

		var tool = new AssignMealTool(scheduler, context);
		var reply = await tool.AssignAsync("Wednesday", recipe.ToString(), "Carbonara", "Dinner", 2026, 19, servings: 4);

		reply.Should().Contain("Carbonara");
		reply.Should().Contain("Wednesday");
		captured.Should().NotBeNull();
		captured!.Year.Should().Be(2026);
		captured.WeekNumber.Should().Be(19);
		captured.Day.Should().Be("Wednesday");
		captured.MealType.Should().Be("Dinner");
		captured.RecipeIdentifier.Should().Be(recipe);
		captured.RecipeTitle.Should().Be("Carbonara");
		captured.Servings.Should().Be(4);
		context.Matches.Should().ContainSingle();
		context.Matches[0].Identifier.Should().Be(recipe);
		context.Matches[0].Reason.Should().Be("Planned for Wednesday · Dinner");
	}

	[Fact]
	public async Task AssignAsync_InvalidGuid_ReturnsErrorWithoutCallingScheduler()
	{
		var tool = new AssignMealTool(scheduler, context);
		var reply = await tool.AssignAsync("Wednesday", "not-a-guid", "Carbonara");

		reply.Should().Contain("Invalid recipe identifier");
		context.Matches.Should().BeEmpty();
		await scheduler.DidNotReceiveWithAnyArgs().AssignAsync(default!, default);
	}

	[Fact]
	public async Task AssignAsync_SchedulerReturnsFalse_ReturnsErrorWithoutAppendingContext()
	{
		scheduler.AssignAsync(Arg.Any<AssignMealRequest>(), Arg.Any<CancellationToken>())
			.Returns(false);

		var tool = new AssignMealTool(scheduler, context);
		var reply = await tool.AssignAsync("Friday", Guid.NewGuid().ToString(), "Pizza");

		reply.Should().Contain("Couldn't plan");
		context.Matches.Should().BeEmpty();
	}

	[Fact]
	public async Task AssignAsync_YearAndWeekZero_ResolvesToCurrentIsoWeek()
	{
		AssignMealRequest? captured = null;
		scheduler.AssignAsync(Arg.Any<AssignMealRequest>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				captured = callInfo.ArgAt<AssignMealRequest>(0);
				return true;
			});

		var tool = new AssignMealTool(scheduler, context);
		await tool.AssignAsync("Monday", Guid.NewGuid().ToString(), "Stew", year: 0, weekNumber: 0);

		captured.Should().NotBeNull();
		captured!.Year.Should().BeGreaterThanOrEqualTo(2026);
		captured.WeekNumber.Should().BeInRange(1, 53);
	}
}
