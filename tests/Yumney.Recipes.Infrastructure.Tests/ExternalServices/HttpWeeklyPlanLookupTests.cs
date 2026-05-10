using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.MealPlan.Client;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure.ExternalServices;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Tests.ExternalServices;

public class HttpWeeklyPlanLookupTests
{
	private readonly IMealPlanClient client = Substitute.For<IMealPlanClient>();

	[Fact]
	public async Task GetForWeekAsync_ClientReturnsResponse_MapsToLookupResult()
	{
		var recipe = Guid.NewGuid();
		client.GetWeeklyPlanAsync(2026, 19, Arg.Any<CancellationToken>())
			.Returns(new WeeklyPlanResponse(
				"2026-W19",
				IsExtendedMode: true,
				[new WeeklyPlanSlotResponse("Monday", "Dinner", "Recipe", "Planned", recipe, "Carbonara", 4, IsEmpty: false)]));

		var lookup = new HttpWeeklyPlanLookup(client);
		var result = await lookup.GetForWeekAsync(2026, 19);

		result.Should().NotBeNull();
		result!.Week.Should().Be("2026-W19");
		result.IsExtendedMode.Should().BeTrue();
		result.Slots.Should().ContainSingle();
		result.Slots[0].RecipeIdentifier.Should().Be(recipe);
		result.Slots[0].RecipeTitle.Should().Be("Carbonara");
		result.Slots[0].IsEmpty.Should().BeFalse();
	}

	[Fact]
	public async Task GetForWeekAsync_ClientReturnsNull_ReturnsNull()
	{
		client.GetWeeklyPlanAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
			.Returns((WeeklyPlanResponse?)null);

		var lookup = new HttpWeeklyPlanLookup(client);
		var result = await lookup.GetForWeekAsync(2026, 19);

		result.Should().BeNull();
	}

	[Fact]
	public async Task GetForWeekAsync_ForwardsYearAndWeekToClient()
	{
		client.GetWeeklyPlanAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
			.Returns((WeeklyPlanResponse?)null);

		var lookup = new HttpWeeklyPlanLookup(client);
		await lookup.GetForWeekAsync(2026, 23);

		await client.Received(1).GetWeeklyPlanAsync(2026, 23, Arg.Any<CancellationToken>());
	}
}
