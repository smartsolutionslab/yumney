using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.MealPlan.Client;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure.ExternalServices;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Tests.ExternalServices;

public class HttpMealConfirmationTests
{
	private readonly IMealPlanClient client = Substitute.For<IMealPlanClient>();

	[Fact]
	public async Task ConfirmAsync_MapsConsumerRequestToClientBody()
	{
		ConfirmMealBody? captured = null;
		var capturedYear = 0;
		var capturedWeek = 0;
		client.ConfirmMealAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<ConfirmMealBody>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				capturedYear = callInfo.ArgAt<int>(0);
				capturedWeek = callInfo.ArgAt<int>(1);
				captured = callInfo.ArgAt<ConfirmMealBody>(2);
				return true;
			});

		var confirmation = new HttpMealConfirmation(client);
		await confirmation.ConfirmAsync(new ConfirmMealRequest(2026, 19, "Wednesday", "Dinner", "Cooked"));

		capturedYear.Should().Be(2026);
		capturedWeek.Should().Be(19);
		captured.Should().NotBeNull();
		captured!.Day.Should().Be("Wednesday");
		captured.MealType.Should().Be("Dinner");
		captured.State.Should().Be("Cooked");
	}

	[Fact]
	public async Task ConfirmAsync_ClientReturnsFalse_PropagatesFalse()
	{
		client.ConfirmMealAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<ConfirmMealBody>(), Arg.Any<CancellationToken>())
			.Returns(false);

		var confirmation = new HttpMealConfirmation(client);
		var result = await confirmation.ConfirmAsync(new ConfirmMealRequest(2026, 19, "Friday", "Lunch", "Skipped"));

		result.Should().BeFalse();
	}
}
