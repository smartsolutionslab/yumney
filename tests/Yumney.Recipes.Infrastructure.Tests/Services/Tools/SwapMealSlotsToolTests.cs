using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Extraction.Services.Tools;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Tests.Services.Tools;

public class SwapMealSlotsToolTests
{
	private readonly IMealSlotSwapper swapper = Substitute.For<IMealSlotSwapper>();

	[Fact]
	public async Task SwapAsync_HappyPath_DispatchesAndReturnsConfirmation()
	{
		SwapMealSlotsRequest? captured = null;
		swapper.SwapAsync(Arg.Any<SwapMealSlotsRequest>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				captured = callInfo.ArgAt<SwapMealSlotsRequest>(0);
				return true;
			});

		var tool = new SwapMealSlotsTool(swapper);
		var reply = await tool.SwapAsync("Thursday", "Friday", "Dinner", 2026, 19);

		reply.Should().Contain("Thursday").And.Contain("Friday");
		captured.Should().NotBeNull();
		captured!.Year.Should().Be(2026);
		captured.WeekNumber.Should().Be(19);
		captured.SourceDay.Should().Be("Thursday");
		captured.TargetDay.Should().Be("Friday");
		captured.MealType.Should().Be("Dinner");
	}

	[Fact]
	public async Task SwapAsync_YearAndWeekZero_ResolvesToCurrentIsoWeek()
	{
		SwapMealSlotsRequest? captured = null;
		swapper.SwapAsync(Arg.Any<SwapMealSlotsRequest>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				captured = callInfo.ArgAt<SwapMealSlotsRequest>(0);
				return true;
			});

		var tool = new SwapMealSlotsTool(swapper);
		await tool.SwapAsync("Monday", "Tuesday");

		captured.Should().NotBeNull();
		captured!.Year.Should().BeGreaterThanOrEqualTo(2026);
		captured.WeekNumber.Should().BeInRange(1, 53);
	}

	[Fact]
	public async Task SwapAsync_SwapperFails_ReturnsErrorMessage()
	{
		swapper.SwapAsync(Arg.Any<SwapMealSlotsRequest>(), Arg.Any<CancellationToken>()).Returns(false);

		var tool = new SwapMealSlotsTool(swapper);
		var reply = await tool.SwapAsync("Monday", "Tuesday");

		reply.Should().Contain("Couldn't swap");
	}
}
