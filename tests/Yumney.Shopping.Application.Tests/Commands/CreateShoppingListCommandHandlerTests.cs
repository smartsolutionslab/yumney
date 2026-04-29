using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shopping.Application.Commands;
using SmartSolutionsLab.Yumney.Shopping.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;
using ShoppingListItem = SmartSolutionsLab.Yumney.Shopping.Application.Commands.ShoppingListItem;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Tests.Commands;

public class CreateShoppingListCommandHandlerTests
{
	private readonly IShoppingListEventStore eventStore = Substitute.For<IShoppingListEventStore>();
	private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
	private readonly CreateShoppingListCommandHandler handler;

	public CreateShoppingListCommandHandlerTests()
	{
		currentUser.UserId.Returns("user-123");
		handler = new CreateShoppingListCommandHandler(eventStore, currentUser);
	}

	[Fact]
	public async Task HandleAsync_ValidCommand_ReturnsSuccess()
	{
		var command = CreateValidCommand();

		var result = await handler.HandleAsync(command);

		result.IsSuccess.Should().BeTrue();
	}

	[Fact]
	public async Task HandleAsync_ValidCommand_ReturnsDetailDto()
	{
		var command = CreateValidCommand();

		var result = await handler.HandleAsync(command);

		result.Value.Title.Should().Be("Weekly Groceries");
		result.Value.Identifier.Should().NotBeEmpty();
		result.Value.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
	}

	[Fact]
	public async Task HandleAsync_ValidCommand_CallsSaveAsyncOnEventStore()
	{
		var command = CreateValidCommand();

		await handler.HandleAsync(command);

		await eventStore.Received(1).SaveAsync(
			Arg.Any<ShoppingList>(),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_EightItems_AllTransferred()
	{
		var command = new CreateShoppingListCommand(
			ShoppingListTitle.From("Big List"),
			[
				new ShoppingListItem(ItemName.From("Flour"), Quantity.Of(Amount.From(500), Unit.Gram)),
				new ShoppingListItem(ItemName.From("Sugar"), Quantity.Of(Amount.From(200), Unit.Gram)),
				new ShoppingListItem(ItemName.From("Butter"), Quantity.Of(Amount.From(250), Unit.Gram)),
				new ShoppingListItem(ItemName.From("Eggs"), Quantity.Of(Amount.From(6), null)),
				new ShoppingListItem(ItemName.From("Milk"), Quantity.Of(Amount.From(1), Unit.Liter)),
				new ShoppingListItem(ItemName.From("Salt"), null),
				new ShoppingListItem(ItemName.From("Pepper"), null),
				new ShoppingListItem(ItemName.From("Vanilla"), Quantity.Of(Amount.From(1), Unit.Teaspoon))
			]);

		var result = await handler.HandleAsync(command);

		result.Value.Items.Should().HaveCount(8);
	}

	[Fact]
	public async Task HandleAsync_UsesCurrentUserAsOwner()
	{
		currentUser.UserId.Returns("specific-user-id");
		ShoppingList? capturedList = null;
		await eventStore.SaveAsync(
			Arg.Do<ShoppingList>(l => capturedList = l),
			Arg.Any<CancellationToken>());

		var command = CreateValidCommand();

		await handler.HandleAsync(command);

		capturedList.Should().NotBeNull();
		capturedList!.Owner.Should().Be(OwnerIdentifier.From("specific-user-id"));
	}

	[Fact]
	public async Task HandleAsync_ForwardsCancellationToken()
	{
		var cts = new CancellationTokenSource();
		var command = CreateValidCommand();

		await handler.HandleAsync(command, cts.Token);

		await eventStore.Received(1).SaveAsync(Arg.Any<ShoppingList>(), cts.Token);
	}

	[Fact]
	public async Task HandleAsync_WithRecipeReference_IncludesInDto()
	{
		var recipeId = Guid.NewGuid();
		var command = new CreateShoppingListCommand(
			ShoppingListTitle.From("From Recipe"),
			[new ShoppingListItem(ItemName.From("Flour"), Quantity.Of(Amount.From(500), Unit.Gram))],
			RecipeReference.From(recipeId));

		var result = await handler.HandleAsync(command);

		result.Value.RecipeReference.Should().Be(recipeId);
	}

	[Fact]
	public async Task HandleAsync_ItemsPreserveAmountAndUnit()
	{
		var command = new CreateShoppingListCommand(
			ShoppingListTitle.From("Test"),
			[
				new ShoppingListItem(ItemName.From("Flour"), Quantity.Of(Amount.From(500), Unit.Gram)),
				new ShoppingListItem(ItemName.From("Salt"), null)
			]);

		var result = await handler.HandleAsync(command);

		result.Value.Items[0].Name.Should().Be("Flour");
		result.Value.Items[0].Amount.Should().Be(500);
		result.Value.Items[0].Unit.Should().Be("g");
		result.Value.Items[1].Name.Should().Be("Salt");
		result.Value.Items[1].Amount.Should().BeNull();
		result.Value.Items[1].Unit.Should().BeNull();
	}

	private static CreateShoppingListCommand CreateValidCommand()
	{
		return new CreateShoppingListCommand(
			ShoppingListTitle.From("Weekly Groceries"),
			[
				new ShoppingListItem(
					ItemName.From("Spaghetti"),
					Quantity.Of(Amount.From(400), Unit.Gram))
			]);
	}
}
