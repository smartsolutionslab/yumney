using FluentAssertions;
using SmartSolutionsLab.Yumney.MealPlan.Application.Commands;
using SmartSolutionsLab.Yumney.MealPlan.Application.Commands.Handlers;
using Xunit;
using static SmartSolutionsLab.Yumney.MealPlan.Application.Tests.MealPlanTestFixture;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Tests.Commands;

public class ToggleExtendedModeCommandHandlerTests
{
	private readonly FakeMealPlanEventStore eventStore = new();
	private readonly ToggleExtendedModeCommandHandler handler;

	public ToggleExtendedModeCommandHandlerTests()
	{
		handler = new ToggleExtendedModeCommandHandler(eventStore, CreateCurrentUser());
	}

	[Fact]
	public async Task HandleAsync_EnableNoPlan_CreatesExtended()
	{
		var result = await handler.HandleAsync(new ToggleExtendedModeCommand(TestWeek, true));

		result.IsSuccess.Should().BeTrue();
		result.Value.IsExtendedMode.Should().BeTrue();
		result.Value.Slots.Should().HaveCount(21);
		eventStore.SaveCount.Should().Be(1);
	}

	[Fact]
	public async Task HandleAsync_DisableExisting_Returns7Slots()
	{
		var existing = CreatePlan();
		existing.EnableExtendedMode();
		eventStore.Seed(existing);

		var result = await handler.HandleAsync(new ToggleExtendedModeCommand(TestWeek, false));

		result.IsSuccess.Should().BeTrue();
		result.Value.IsExtendedMode.Should().BeFalse();
		result.Value.Slots.Should().HaveCount(7);
	}
}
