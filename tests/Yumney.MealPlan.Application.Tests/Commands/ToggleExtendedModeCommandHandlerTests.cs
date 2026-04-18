using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.MealPlan.Application.Commands;
using SmartSolutionsLab.Yumney.MealPlan.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Common;
using Xunit;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Tests.Commands;

public class ToggleExtendedModeCommandHandlerTests
{
	private readonly IWeeklyPlanRepository plans = Substitute.For<IWeeklyPlanRepository>();
	private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
	private readonly ToggleExtendedModeCommandHandler handler;

	public ToggleExtendedModeCommandHandlerTests()
	{
		currentUser.UserId.Returns("user-123");
		handler = new ToggleExtendedModeCommandHandler(plans, currentUser);
	}

	[Fact]
	public async Task HandleAsync_EnableNoPlan_CreatesExtended()
	{
		plans.FindByOwnerAndWeekAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<WeekIdentifier>(), Arg.Any<CancellationToken>())
			.Returns((WeeklyPlan?)null);

		var result = await handler.HandleAsync(new ToggleExtendedModeCommand(WeekIdentifier.From(2026, 15), true));

		result.IsSuccess.Should().BeTrue();
		result.Value.IsExtendedMode.Should().BeTrue();
		result.Value.Slots.Should().HaveCount(21);
		await plans.Received(1).AddAsync(Arg.Any<WeeklyPlan>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_DisableExisting_Returns7Slots()
	{
		var owner = OwnerIdentifier.From("user-123");
		var week = WeekIdentifier.From(2026, 15);
		var existing = WeeklyPlan.Create(owner, week);
		existing.EnableExtendedMode();

		plans.FindByOwnerAndWeekAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<WeekIdentifier>(), Arg.Any<CancellationToken>())
			.Returns(existing);
		plans.GetByOwnerAndWeekAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<WeekIdentifier>(), Arg.Any<CancellationToken>())
			.Returns(existing);

		var result = await handler.HandleAsync(new ToggleExtendedModeCommand(WeekIdentifier.From(2026, 15), false));

		result.IsSuccess.Should().BeTrue();
		result.Value.IsExtendedMode.Should().BeFalse();
		result.Value.Slots.Should().HaveCount(7);
	}
}
