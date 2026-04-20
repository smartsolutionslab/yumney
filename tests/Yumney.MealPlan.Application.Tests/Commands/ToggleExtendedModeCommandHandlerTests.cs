using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.MealPlan.Application.Commands;
using SmartSolutionsLab.Yumney.MealPlan.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Common;
using Xunit;
using static SmartSolutionsLab.Yumney.MealPlan.Application.Tests.MealPlanTestFixture;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Tests.Commands;

public class ToggleExtendedModeCommandHandlerTests
{
	private readonly IWeeklyPlanRepository plans = Substitute.For<IWeeklyPlanRepository>();
	private readonly ToggleExtendedModeCommandHandler handler;

	public ToggleExtendedModeCommandHandlerTests()
	{
		handler = new ToggleExtendedModeCommandHandler(plans, CreateCurrentUser());
	}

	[Fact]
	public async Task HandleAsync_EnableNoPlan_CreatesExtended()
	{
		plans.FindForUpdateAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<WeekIdentifier>(), Arg.Any<CancellationToken>())
			.Returns((WeeklyPlan?)null);

		var result = await handler.HandleAsync(new ToggleExtendedModeCommand(TestWeek, true));

		result.IsSuccess.Should().BeTrue();
		result.Value.IsExtendedMode.Should().BeTrue();
		result.Value.Slots.Should().HaveCount(21);
		await plans.Received(1).AddAsync(Arg.Any<WeeklyPlan>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_DisableExisting_Returns7Slots()
	{
		var existing = CreatePlan();
		existing.EnableExtendedMode();

		plans.FindForUpdateAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<WeekIdentifier>(), Arg.Any<CancellationToken>())
			.Returns(existing);

		var result = await handler.HandleAsync(new ToggleExtendedModeCommand(TestWeek, false));

		result.IsSuccess.Should().BeTrue();
		result.Value.IsExtendedMode.Should().BeFalse();
		result.Value.Slots.Should().HaveCount(7);
	}
}
