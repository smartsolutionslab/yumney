using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shopping.Application.Commands;
using SmartSolutionsLab.Yumney.Shopping.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Tests.Commands;

public class CheckOffItemCommandHandlerTests
{
	private readonly IShoppingListEventStore eventStore = Substitute.For<IShoppingListEventStore>();
	private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
	private readonly CheckOffItemCommandHandler handler;

	public CheckOffItemCommandHandlerTests()
	{
		currentUser.UserId.Returns("user-123");
		handler = new CheckOffItemCommandHandler(eventStore, currentUser);
	}

	[Fact]
	public async Task HandleAsync_ValidCheckCommand_ReturnsSuccess()
	{
		var list = ShoppingListTestData.CreateListWithItem("user-123", out var itemId);
		eventStore.LoadAsync(list.Identifier, Arg.Any<CancellationToken>()).Returns(list);
		var command = new CheckOffItemCommand(list.Identifier, itemId, true);

		var result = await handler.HandleAsync(command);

		result.IsSuccess.Should().BeTrue();
	}

	[Fact]
	public async Task HandleAsync_ValidUncheckCommand_ReturnsSuccess()
	{
		var list = ShoppingListTestData.CreateListWithItem("user-123", out var itemId);
		eventStore.LoadAsync(list.Identifier, Arg.Any<CancellationToken>()).Returns(list);
		var command = new CheckOffItemCommand(list.Identifier, itemId, false);

		var result = await handler.HandleAsync(command);

		result.IsSuccess.Should().BeTrue();
	}

	[Fact]
	public async Task HandleAsync_ListNotFound_ThrowsEntityNotFoundException()
	{
		var listId = ShoppingListIdentifier.New();
		eventStore.LoadAsync(listId, Arg.Any<CancellationToken>())
			.ThrowsAsync(new EntityNotFoundException(nameof(ShoppingList), listId.Value));
		var command = new CheckOffItemCommand(listId, ShoppingListItemIdentifier.New(), true);

		var act = () => handler.HandleAsync(command);

		await act.Should().ThrowAsync<EntityNotFoundException>();
	}

	[Fact]
	public async Task HandleAsync_DifferentOwner_ReturnsAccessDenied()
	{
		var list = ShoppingListTestData.CreateListWithItem("other-user", out var itemId);
		eventStore.LoadAsync(list.Identifier, Arg.Any<CancellationToken>()).Returns(list);
		var command = new CheckOffItemCommand(list.Identifier, itemId, true);

		var result = await handler.HandleAsync(command);

		result.IsSuccess.Should().BeFalse();
		result.Error.Should().Be(CheckOffItemErrors.AccessDenied);
	}

	[Fact]
	public async Task HandleAsync_ValidCommand_CallsSaveAsync()
	{
		var list = ShoppingListTestData.CreateListWithItem("user-123", out var itemId);
		eventStore.LoadAsync(list.Identifier, Arg.Any<CancellationToken>()).Returns(list);
		var command = new CheckOffItemCommand(list.Identifier, itemId, true);

		await handler.HandleAsync(command);

		await eventStore.Received(1).SaveAsync(list, Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_CheckTrue_MarksItemChecked()
	{
		var list = ShoppingListTestData.CreateListWithItem("user-123", out var itemId);
		eventStore.LoadAsync(list.Identifier, Arg.Any<CancellationToken>()).Returns(list);
		var command = new CheckOffItemCommand(list.Identifier, itemId, true);

		await handler.HandleAsync(command);

		list.Items.First(item => item.Id == itemId).IsChecked.Should().BeTrue();
	}

	[Fact]
	public async Task HandleAsync_CheckFalse_MarksItemUnchecked()
	{
		var list = ShoppingListTestData.CreateListWithItem("user-123", out var itemId);
		list.CheckOffItem(itemId);
		eventStore.LoadAsync(list.Identifier, Arg.Any<CancellationToken>()).Returns(list);
		var command = new CheckOffItemCommand(list.Identifier, itemId, false);

		await handler.HandleAsync(command);

		list.Items.First(item => item.Id == itemId).IsChecked.Should().BeFalse();
	}

	[Fact]
	public async Task HandleAsync_ForwardsCancellationToken()
	{
		var cts = new CancellationTokenSource();
		var list = ShoppingListTestData.CreateListWithItem("user-123", out var itemId);
		eventStore.LoadAsync(list.Identifier, Arg.Any<CancellationToken>()).Returns(list);
		var command = new CheckOffItemCommand(list.Identifier, itemId, true);

		await handler.HandleAsync(command, cts.Token);

		await eventStore.Received(1).LoadAsync(list.Identifier, cts.Token);
		await eventStore.Received(1).SaveAsync(list, cts.Token);
	}
}
