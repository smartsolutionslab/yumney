using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Extraction.Services.Tools;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Tests.Services.Tools;

public class ClearMealSlotToolTests
{
	private readonly IMealSlotClearer clearer = Substitute.For<IMealSlotClearer>();

	[Fact]
	public async Task ClearAsync_HappyPath_DispatchesAndReturnsConfirmation()
	{
		ClearMealSlotRequest? captured = null;
		clearer.ClearAsync(Arg.Any<ClearMealSlotRequest>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				captured = callInfo.ArgAt<ClearMealSlotRequest>(0);
				return true;
			});

		var tool = new ClearMealSlotTool(clearer);
		var reply = await tool.ClearAsync("Wednesday", "Dinner", 2026, 19);

		reply.Should().Contain("Cleared Wednesday");
		captured.Should().NotBeNull();
		captured!.Year.Should().Be(2026);
		captured.WeekNumber.Should().Be(19);
		captured.Day.Should().Be("Wednesday");
		captured.MealType.Should().Be("Dinner");
	}

	[Fact]
	public async Task ClearAsync_YearAndWeekZero_ResolvesToCurrentIsoWeek()
	{
		ClearMealSlotRequest? captured = null;
		clearer.ClearAsync(Arg.Any<ClearMealSlotRequest>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				captured = callInfo.ArgAt<ClearMealSlotRequest>(0);
				return true;
			});

		var tool = new ClearMealSlotTool(clearer);
		await tool.ClearAsync("Monday");

		captured.Should().NotBeNull();
		captured!.Year.Should().BeGreaterThanOrEqualTo(2026);
		captured.WeekNumber.Should().BeInRange(1, 53);
	}

	[Fact]
	public async Task ClearAsync_ClearerFails_ReturnsAlreadyEmptyMessage()
	{
		clearer.ClearAsync(Arg.Any<ClearMealSlotRequest>(), Arg.Any<CancellationToken>()).Returns(false);

		var tool = new ClearMealSlotTool(clearer);
		var reply = await tool.ClearAsync("Friday");

		reply.Should().Contain("already be empty");
	}
}
