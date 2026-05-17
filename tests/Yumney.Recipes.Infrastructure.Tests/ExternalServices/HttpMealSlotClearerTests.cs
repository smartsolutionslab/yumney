using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.MealPlan.Client;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure.ExternalServices;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Tests.ExternalServices;

public class HttpMealSlotClearerTests
{
	private readonly IMealPlanClient client = Substitute.For<IMealPlanClient>();

	[Fact]
	public async Task ClearAsync_MapsConsumerRequestToClientBody()
	{
		ClearSlotBody? captured = null;
		var capturedYear = 0;
		var capturedWeek = 0;
		client.ClearSlotAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<ClearSlotBody>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				capturedYear = callInfo.ArgAt<int>(0);
				capturedWeek = callInfo.ArgAt<int>(1);
				captured = callInfo.ArgAt<ClearSlotBody>(2);
				return true;
			});

		var clearer = new HttpMealSlotClearer(client);
		await clearer.ClearAsync(new ClearMealSlotRequest(2026, 19, "Wednesday", "Dinner"));

		capturedYear.Should().Be(2026);
		capturedWeek.Should().Be(19);
		captured.Should().NotBeNull();
		captured!.Day.Should().Be("Wednesday");
		captured.MealType.Should().Be("Dinner");
	}

	[Fact]
	public async Task ClearAsync_ClientReturnsFalse_PropagatesFalse()
	{
		client.ClearSlotAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<ClearSlotBody>(), Arg.Any<CancellationToken>())
			.Returns(false);

		var clearer = new HttpMealSlotClearer(client);
		var result = await clearer.ClearAsync(new ClearMealSlotRequest(2026, 19, "Friday", "Lunch"));

		result.Should().BeFalse();
	}
}
