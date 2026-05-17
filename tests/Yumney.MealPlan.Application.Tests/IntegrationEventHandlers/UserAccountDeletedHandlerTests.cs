using NSubstitute;
using SmartSolutionsLab.Yumney.MealPlan.Application.IntegrationEventHandlers;
using SmartSolutionsLab.Yumney.MealPlan.Application.Interfaces;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Events.Contracts;
using Xunit;

namespace SmartSolutionsLab.Yumney.MealPlan.Application.Tests.IntegrationEventHandlers;

public class UserAccountDeletedHandlerTests
{
	private readonly IMealPlanUserDataPurger purger = Substitute.For<IMealPlanUserDataPurger>();
	private readonly UserAccountDeletedHandler handler;

	public UserAccountDeletedHandlerTests()
	{
		handler = new UserAccountDeletedHandler(purger);
	}

	[Fact]
	public async Task HandleAsync_DelegatesToPurger_WithOwnerProjectedFromKeycloakId()
	{
		var @event = new UserAccountDeletedIntegrationEvent("kc-user-1");

		await handler.HandleAsync(@event);

		await purger.Received(1).PurgeAsync(OwnerIdentifier.From("kc-user-1"), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_ForwardsCancellationToken()
	{
		var @event = new UserAccountDeletedIntegrationEvent("kc-user-1");
		using var cts = new CancellationTokenSource();

		await handler.HandleAsync(@event, cts.Token);

		await purger.Received(1).PurgeAsync(Arg.Any<OwnerIdentifier>(), cts.Token);
	}
}
