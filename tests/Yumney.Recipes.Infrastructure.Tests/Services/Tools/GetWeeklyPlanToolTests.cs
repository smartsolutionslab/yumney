using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Extraction.Services;
using SmartSolutionsLab.Yumney.Recipes.Extraction.Services.Tools;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Tests.Services.Tools;

public class GetWeeklyPlanToolTests
{
	private readonly IWeeklyPlanLookup lookup = Substitute.For<IWeeklyPlanLookup>();
	private readonly ChatToolContext context = new();

	[Fact]
	public async Task GetAsync_PlanFound_AppendsRecipeMatchesPerSlot()
	{
		var first = Guid.NewGuid();
		var second = Guid.NewGuid();
		lookup.GetForWeekAsync(2026, 19, Arg.Any<CancellationToken>())
			.Returns(new WeeklyPlanLookupResult(
				"2026-W19",
				IsExtendedMode: false,
				[
					new WeeklyPlanLookupSlot("Monday", "Dinner", first, "Carbonara", 4, IsEmpty: false),
					new WeeklyPlanLookupSlot("Tuesday", "Dinner", second, "Risotto", 4, IsEmpty: false),
					new WeeklyPlanLookupSlot("Wednesday", "Dinner", null, null, 0, IsEmpty: true),
				]));

		var tool = new GetWeeklyPlanTool(lookup, context);
		var result = await tool.GetAsync(2026, 19);

		result.Should().NotBeNull();
		result!.Slots.Should().HaveCount(3);
		context.Matches.Should().HaveCount(2);
		context.Matches.Select(match => match.Identifier).Should().ContainInOrder(first, second);
		context.Matches[0].Reason.Should().Be("Monday · Dinner");
	}

	[Fact]
	public async Task GetAsync_PlanNotFound_ReturnsNullWithoutAppending()
	{
		lookup.GetForWeekAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
			.Returns((WeeklyPlanLookupResult?)null);

		var tool = new GetWeeklyPlanTool(lookup, context);
		var result = await tool.GetAsync(2026, 19);

		result.Should().BeNull();
		context.Matches.Should().BeEmpty();
	}

	[Fact]
	public async Task GetAsync_YearAndWeekZero_ResolvesToCurrentIsoWeek()
	{
		WeeklyPlanLookupResult? captured = null;
		int capturedYear = 0;
		int capturedWeek = 0;
		lookup.GetForWeekAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				capturedYear = callInfo.ArgAt<int>(0);
				capturedWeek = callInfo.ArgAt<int>(1);
				captured = new WeeklyPlanLookupResult($"{capturedYear}-W{capturedWeek}", false, []);
				return captured;
			});

		var tool = new GetWeeklyPlanTool(lookup, context);
		await tool.GetAsync(0, 0);

		capturedYear.Should().BeGreaterThanOrEqualTo(2026);
		capturedWeek.Should().BeInRange(1, 53);
	}

	[Fact]
	public async Task GetAsync_EmptySlot_DoesNotAppendRecipeMatch()
	{
		lookup.GetForWeekAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
			.Returns(new WeeklyPlanLookupResult(
				"2026-W19",
				IsExtendedMode: false,
				[new WeeklyPlanLookupSlot("Friday", "Dinner", null, null, 0, IsEmpty: true)]));

		var tool = new GetWeeklyPlanTool(lookup, context);
		await tool.GetAsync(2026, 19);

		context.Matches.Should().BeEmpty();
	}
}
