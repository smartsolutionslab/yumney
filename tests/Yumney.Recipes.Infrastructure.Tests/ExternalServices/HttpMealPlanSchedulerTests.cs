using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.MealPlan.Client;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure.ExternalServices;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Tests.ExternalServices;

public class HttpMealPlanSchedulerTests
{
	private readonly IMealPlanClient client = Substitute.For<IMealPlanClient>();

	[Fact]
	public async Task AssignAsync_MapsConsumerRequestToClientBody()
	{
		AssignRecipeBody? captured = null;
		var capturedYear = 0;
		var capturedWeek = 0;
		client.AssignRecipeAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<AssignRecipeBody>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				capturedYear = callInfo.ArgAt<int>(0);
				capturedWeek = callInfo.ArgAt<int>(1);
				captured = callInfo.ArgAt<AssignRecipeBody>(2);
				return true;
			});

		var scheduler = new HttpMealPlanScheduler(client);
		var recipe = Guid.NewGuid();
		await scheduler.AssignAsync(new AssignMealRequest(2026, 19, "Wednesday", "Dinner", recipe, "Carbonara", Servings: 4));

		capturedYear.Should().Be(2026);
		capturedWeek.Should().Be(19);
		captured.Should().NotBeNull();
		captured!.Day.Should().Be("Wednesday");
		captured.MealType.Should().Be("Dinner");
		captured.RecipeIdentifier.Should().Be(recipe);
		captured.RecipeTitle.Should().Be("Carbonara");
		captured.Servings.Should().Be(4);
	}

	[Fact]
	public async Task AssignAsync_ClientReturnsFalse_PropagatesFalse()
	{
		client.AssignRecipeAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<AssignRecipeBody>(), Arg.Any<CancellationToken>())
			.Returns(false);

		var scheduler = new HttpMealPlanScheduler(client);
		var result = await scheduler.AssignAsync(new AssignMealRequest(2026, 19, "Friday", "Lunch", Guid.NewGuid(), "Pizza", Servings: null));

		result.Should().BeFalse();
	}
}
