using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shopping.Application.Commands;
using SmartSolutionsLab.Yumney.Shopping.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Tests.Commands;

public class CheckOffAllItemsCommandHandlerTests
{
	private readonly IShoppingListEventStore eventStore = Substitute.For<IShoppingListEventStore>();
	private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
	private readonly CheckOffAllItemsCommandHandler handler;

	public CheckOffAllItemsCommandHandlerTests()
	{
		currentUser.UserId.Returns("user-123");
		handler = new CheckOffAllItemsCommandHandler(eventStore, currentUser);
	}

	[Fact]
	public async Task HandleAsync_CheckAllValid_ReturnsSuccess()
	{
		var list = ShoppingListTestData.CreateListWithItems();
		eventStore.LoadAsync(list.Identifier, Arg.Any<CancellationToken>()).Returns(list);
		var command = new CheckOffAllItemsCommand(list.Identifier, true);

		var result = await handler.HandleAsync(command);

		result.IsSuccess.Should().BeTrue();
	}

	[Fact]
	public async Task HandleAsync_UncheckAllValid_ReturnsSuccess()
	{
		var list = ShoppingListTestData.CreateListWithItems();
		eventStore.LoadAsync(list.Identifier, Arg.Any<CancellationToken>()).Returns(list);
		var command = new CheckOffAllItemsCommand(list.Identifier, false);

		var result = await handler.HandleAsync(command);

		result.IsSuccess.Should().BeTrue();
	}

	[Fact]
	public async Task HandleAsync_ListNotFound_ThrowsEntityNotFoundException()
	{
		var listId = ShoppingListIdentifier.New();
		eventStore.LoadAsync(listId, Arg.Any<CancellationToken>()).Returns((ShoppingList?)null);
		var command = new CheckOffAllItemsCommand(listId, true);

		var act = () => handler.HandleAsync(command);

		await act.Should().ThrowAsync<EntityNotFoundException>();
	}

	[Fact]
	public async Task HandleAsync_DifferentOwner_ReturnsAccessDenied()
	{
		var list = ShoppingListTestData.CreateListWithItems("other-user");
		eventStore.LoadAsync(list.Identifier, Arg.Any<CancellationToken>()).Returns(list);
		var command = new CheckOffAllItemsCommand(list.Identifier, true);

		var result = await handler.HandleAsync(command);

		result.IsSuccess.Should().BeFalse();
		result.Error.Should().Be(CheckOffItemErrors.AccessDenied);
	}

	[Fact]
	public async Task HandleAsync_CheckTrue_AllItemsChecked()
	{
		var list = ShoppingListTestData.CreateListWithItems();
		eventStore.LoadAsync(list.Identifier, Arg.Any<CancellationToken>()).Returns(list);
		var command = new CheckOffAllItemsCommand(list.Identifier, true);

		await handler.HandleAsync(command);

		list.Items.Should().AllSatisfy(item => item.IsChecked.Should().BeTrue());
	}

	[Fact]
	public async Task HandleAsync_CheckFalse_AllItemsUnchecked()
	{
		var list = ShoppingListTestData.CreateListWithItems();
		list.CheckAllItems();
		eventStore.LoadAsync(list.Identifier, Arg.Any<CancellationToken>()).Returns(list);
		var command = new CheckOffAllItemsCommand(list.Identifier, false);

		await handler.HandleAsync(command);

		list.Items.Should().AllSatisfy(item => item.IsChecked.Should().BeFalse());
	}

	[Fact]
	public async Task HandleAsync_ValidCommand_CallsSaveAsync()
	{
		var list = ShoppingListTestData.CreateListWithItems();
		eventStore.LoadAsync(list.Identifier, Arg.Any<CancellationToken>()).Returns(list);
		var command = new CheckOffAllItemsCommand(list.Identifier, true);

		await handler.HandleAsync(command);

		await eventStore.Received(1).SaveAsync(list, Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_ForwardsCancellationToken()
	{
		var cts = new CancellationTokenSource();
		var list = ShoppingListTestData.CreateListWithItems();
		eventStore.LoadAsync(list.Identifier, Arg.Any<CancellationToken>()).Returns(list);
		var command = new CheckOffAllItemsCommand(list.Identifier, true);

		await handler.HandleAsync(command, cts.Token);

		await eventStore.Received(1).LoadAsync(list.Identifier, cts.Token);
		await eventStore.Received(1).SaveAsync(list, cts.Token);
	}
}
