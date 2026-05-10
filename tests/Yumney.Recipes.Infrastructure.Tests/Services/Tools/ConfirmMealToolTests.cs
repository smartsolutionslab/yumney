using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Extraction.Services.Tools;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Tests.Services.Tools;

public class ConfirmMealToolTests
{
	private readonly IMealConfirmation confirmation = Substitute.For<IMealConfirmation>();

	[Fact]
	public async Task ConfirmAsync_HappyPath_ForwardsRequest()
	{
		ConfirmMealRequest? captured = null;
		confirmation.ConfirmAsync(Arg.Any<ConfirmMealRequest>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				captured = callInfo.ArgAt<ConfirmMealRequest>(0);
				return true;
			});

		var tool = new ConfirmMealTool(confirmation);
		var reply = await tool.ConfirmAsync("Wednesday", "Cooked", "Dinner", 2026, 19);

		reply.Should().Contain("Wednesday");
		reply.Should().Contain("cooked");
		captured.Should().NotBeNull();
		captured!.Year.Should().Be(2026);
		captured.WeekNumber.Should().Be(19);
		captured.Day.Should().Be("Wednesday");
		captured.MealType.Should().Be("Dinner");
		captured.State.Should().Be("Cooked");
	}

	[Fact]
	public async Task ConfirmAsync_ConfirmationReturnsFalse_ReturnsErrorMessage()
	{
		confirmation.ConfirmAsync(Arg.Any<ConfirmMealRequest>(), Arg.Any<CancellationToken>())
			.Returns(false);

		var tool = new ConfirmMealTool(confirmation);
		var reply = await tool.ConfirmAsync("Friday", "Skipped");

		reply.Should().Contain("Couldn't update");
	}

	[Fact]
	public async Task ConfirmAsync_YearAndWeekZero_ResolvesToCurrentIsoWeek()
	{
		ConfirmMealRequest? captured = null;
		confirmation.ConfirmAsync(Arg.Any<ConfirmMealRequest>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				captured = callInfo.ArgAt<ConfirmMealRequest>(0);
				return true;
			});

		var tool = new ConfirmMealTool(confirmation);
		await tool.ConfirmAsync("Monday", "Cooked", year: 0, weekNumber: 0);

		captured.Should().NotBeNull();
		captured!.Year.Should().BeGreaterThanOrEqualTo(2026);
		captured.WeekNumber.Should().BeInRange(1, 53);
	}
}
