using NSubstitute;
using SmartSolutionsLab.Yumney.Shared.Events.Contracts;
using SmartSolutionsLab.Yumney.Shopping.Application.IntegrationEventHandlers;
using SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.TestBuilders.Shopping;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Tests.IntegrationEventHandlers;

public class RecipeDeletedHandlerTests
{
	private readonly IShoppingListProjectionRepository projection = Substitute.For<IShoppingListProjectionRepository>();
	private readonly IShoppingListEventStore eventStore = Substitute.For<IShoppingListEventStore>();
	private readonly RecipeDeletedHandler handler;

	public RecipeDeletedHandlerTests()
	{
		handler = new RecipeDeletedHandler(projection, eventStore);
	}

	[Fact]
	public async Task HandleAsync_NoListsReferenceRecipe_DoesNothing()
	{
		var @event = new RecipeDeletedIntegrationEvent("user-123", Guid.NewGuid());
		projection.FindIdsByRecipeAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<RecipeReference>(), Arg.Any<CancellationToken>())
			.Returns([]);

		await handler.HandleAsync(@event);

		await eventStore.DidNotReceiveWithAnyArgs().FindAsync(default!, default);
		await eventStore.DidNotReceiveWithAnyArgs().SaveAsync(default!, default);
	}

	[Fact]
	public async Task HandleAsync_OneMatchingList_ClearsAndSaves()
	{
		var recipeId = Guid.NewGuid();
		var list = ShoppingListBuilder.A().OwnedBy("user-123").FromRecipe(recipeId).Build();
		projection.FindIdsByRecipeAsync(
				OwnerIdentifier.From("user-123"),
				RecipeReference.From(recipeId),
				Arg.Any<CancellationToken>())
			.Returns([list.Identifier]);
		eventStore.FindAsync(list.Identifier, Arg.Any<CancellationToken>()).Returns(list);

		await handler.HandleAsync(new RecipeDeletedIntegrationEvent("user-123", recipeId));

		await eventStore.Received(1).SaveAsync(list, Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_ListVanishedBetweenProjectionAndStore_SkipsItContinues()
	{
		var recipeId = Guid.NewGuid();
		var existing = ShoppingListBuilder.A().OwnedBy("user-123").FromRecipe(recipeId).Build();
		var vanishedId = ShoppingListIdentifier.From(Guid.NewGuid());
		projection.FindIdsByRecipeAsync(
				Arg.Any<OwnerIdentifier>(),
				Arg.Any<RecipeReference>(),
				Arg.Any<CancellationToken>())
			.Returns([vanishedId, existing.Identifier]);
		eventStore.FindAsync(vanishedId, Arg.Any<CancellationToken>()).Returns((ShoppingList?)null);
		eventStore.FindAsync(existing.Identifier, Arg.Any<CancellationToken>()).Returns(existing);

		await handler.HandleAsync(new RecipeDeletedIntegrationEvent("user-123", recipeId));

		await eventStore.Received(1).SaveAsync(existing, Arg.Any<CancellationToken>());
		await eventStore.DidNotReceive().SaveAsync(Arg.Is<ShoppingList>(list => list != existing), Arg.Any<CancellationToken>());
	}
}
