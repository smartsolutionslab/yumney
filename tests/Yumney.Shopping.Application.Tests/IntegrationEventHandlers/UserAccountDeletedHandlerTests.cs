using NSubstitute;
using SmartSolutionsLab.Yumney.Shared.Events.Contracts;
using SmartSolutionsLab.Yumney.Shopping.Application.IntegrationEventHandlers;
using SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Tests.IntegrationEventHandlers;

public class UserAccountDeletedHandlerTests
{
	private readonly IShoppingUserDataPurger purger = Substitute.For<IShoppingUserDataPurger>();
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
