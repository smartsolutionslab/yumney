using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shopping.Application.Commands;
using SmartSolutionsLab.Yumney.Shopping.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList.Events;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Tests.Commands;

public class ChangeItemCategoryCommandHandlerTests
{
	private readonly IShoppingListEventStore eventStore = Substitute.For<IShoppingListEventStore>();
	private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
	private readonly ChangeItemCategoryCommandHandler handler;

	public ChangeItemCategoryCommandHandlerTests()
	{
		currentUser.UserId.Returns("user-123");
		handler = new ChangeItemCategoryCommandHandler(eventStore, currentUser);
	}

	[Fact]
	public async Task HandleAsync_ValidCommand_RaisesCategoryChangedEvent()
	{
		var list = ShoppingListTestData.CreateListWithItem("user-123", out var itemId);
		list.MarkCommitted();
		eventStore.LoadAsync(list.Identifier, Arg.Any<CancellationToken>()).Returns(list);

		await handler.HandleAsync(new ChangeItemCategoryCommand(list.Identifier, itemId, IngredientCategory.Dairy));

		var raised = list.UncommittedEvents.OfType<ListItemCategoryChanged>().Single();
		raised.Category.Should().Be(IngredientCategory.Dairy);
	}

	[Fact]
	public async Task HandleAsync_ValidCommand_PersistsViaEventStore()
	{
		var list = ShoppingListTestData.CreateListWithItem("user-123", out var itemId);
		eventStore.LoadAsync(list.Identifier, Arg.Any<CancellationToken>()).Returns(list);

		await handler.HandleAsync(new ChangeItemCategoryCommand(list.Identifier, itemId, IngredientCategory.Spices));

		await eventStore.Received(1).SaveAsync(list, Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_DifferentOwner_ReturnsAccessDeniedAndDoesNotSave()
	{
		var list = ShoppingListTestData.CreateListWithItem("other-user", out var itemId);
		eventStore.LoadAsync(list.Identifier, Arg.Any<CancellationToken>()).Returns(list);

		var result = await handler.HandleAsync(
			new ChangeItemCategoryCommand(list.Identifier, itemId, IngredientCategory.Bakery));

		result.IsSuccess.Should().BeFalse();
		result.Error.Should().Be(CheckOffItemErrors.AccessDenied);
		await eventStore.DidNotReceive().SaveAsync(Arg.Any<ShoppingList>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_ListNotFound_ThrowsEntityNotFoundException()
	{
		var listId = ShoppingListIdentifier.New();
		eventStore.LoadAsync(listId, Arg.Any<CancellationToken>()).Returns((ShoppingList?)null);

		var act = () => handler.HandleAsync(
			new ChangeItemCategoryCommand(listId, ShoppingListItemIdentifier.New(), IngredientCategory.Pantry));

		await act.Should().ThrowAsync<EntityNotFoundException>();
	}

	[Fact]
	public async Task HandleAsync_ForwardsCancellationToken()
	{
		var cts = new CancellationTokenSource();
		var list = ShoppingListTestData.CreateListWithItem("user-123", out var itemId);
		eventStore.LoadAsync(list.Identifier, Arg.Any<CancellationToken>()).Returns(list);

		await handler.HandleAsync(
			new ChangeItemCategoryCommand(list.Identifier, itemId, IngredientCategory.Beverages),
			cts.Token);

		await eventStore.Received(1).LoadAsync(list.Identifier, cts.Token);
		await eventStore.Received(1).SaveAsync(list, cts.Token);
	}
}
