using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Application.IntegrationEventHandlers;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Events.Contracts;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Tests.IntegrationEventHandlers;

public class UserAccountDeletedHandlerTests
{
	private readonly IRecipesUserDataPurger purger = Substitute.For<IRecipesUserDataPurger>();
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
