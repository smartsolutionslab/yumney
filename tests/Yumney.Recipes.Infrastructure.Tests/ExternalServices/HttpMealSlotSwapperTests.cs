using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.MealPlan.Client;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure.ExternalServices;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Tests.ExternalServices;

public class HttpMealSlotSwapperTests
{
	private readonly IMealPlanClient client = Substitute.For<IMealPlanClient>();

	[Fact]
	public async Task SwapAsync_MapsConsumerRequestToClientBody()
	{
		SwapSlotsBody? captured = null;
		var capturedYear = 0;
		var capturedWeek = 0;
		client.SwapSlotsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<SwapSlotsBody>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				capturedYear = callInfo.ArgAt<int>(0);
				capturedWeek = callInfo.ArgAt<int>(1);
				captured = callInfo.ArgAt<SwapSlotsBody>(2);
				return true;
			});

		var swapper = new HttpMealSlotSwapper(client);
		await swapper.SwapAsync(new SwapMealSlotsRequest(2026, 20, "Monday", "Friday", "Dinner"));

		capturedYear.Should().Be(2026);
		capturedWeek.Should().Be(20);
		captured.Should().NotBeNull();
		captured!.SourceDay.Should().Be("Monday");
		captured.TargetDay.Should().Be("Friday");
		captured.MealType.Should().Be("Dinner");
	}

	[Fact]
	public async Task SwapAsync_ClientReturnsFalse_PropagatesFalse()
	{
		client.SwapSlotsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<SwapSlotsBody>(), Arg.Any<CancellationToken>())
			.Returns(false);

		var swapper = new HttpMealSlotSwapper(client);
		var result = await swapper.SwapAsync(new SwapMealSlotsRequest(2026, 19, "Tuesday", "Thursday", "Lunch"));

		result.Should().BeFalse();
	}
}
