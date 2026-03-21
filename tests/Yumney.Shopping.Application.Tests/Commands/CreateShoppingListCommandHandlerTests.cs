using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shopping.Application.Commands;
using SmartSolutionsLab.Yumney.Shopping.Application.Commands.Handler;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Tests.Commands;

public class CreateShoppingListCommandHandlerTests
{
    private readonly IShoppingListRepository shoppingLists = Substitute.For<IShoppingListRepository>();
    private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
    private readonly ILogger<CreateShoppingListCommandHandler> logger = Substitute.For<ILogger<CreateShoppingListCommandHandler>>();
    private readonly CreateShoppingListCommandHandler handler;

    public CreateShoppingListCommandHandlerTests()
    {
        currentUser.UserId.Returns("user-123");
        handler = new CreateShoppingListCommandHandler(shoppingLists, currentUser, logger);
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
    public async Task HandleAsync_ValidCommand_CallsAddAsync()
    {
        var command = CreateValidCommand();

        await handler.HandleAsync(command);

        await shoppingLists.Received(1).AddAsync(
            Arg.Any<ShoppingList>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_EightItems_AllTransferred()
    {
        var command = new CreateShoppingListCommand(
            new ShoppingListTitle("Big List"),
            [
                new CreateShoppingListItemCommand(new ItemName("Flour"), new Amount(500), new Unit("g")),
                new CreateShoppingListItemCommand(new ItemName("Sugar"), new Amount(200), new Unit("g")),
                new CreateShoppingListItemCommand(new ItemName("Butter"), new Amount(250), new Unit("g")),
                new CreateShoppingListItemCommand(new ItemName("Eggs"), new Amount(6), null),
                new CreateShoppingListItemCommand(new ItemName("Milk"), new Amount(1), new Unit("l")),
                new CreateShoppingListItemCommand(new ItemName("Salt"), null, null),
                new CreateShoppingListItemCommand(new ItemName("Pepper"), null, null),
                new CreateShoppingListItemCommand(new ItemName("Vanilla"), new Amount(1), new Unit("tsp")),
            ]);

        var result = await handler.HandleAsync(command);

        result.Value.Items.Should().HaveCount(8);
    }

    [Fact]
    public async Task HandleAsync_UsesCurrentUserAsOwner()
    {
        currentUser.UserId.Returns("specific-user-id");
        ShoppingList? capturedList = null;
        await shoppingLists.AddAsync(
            Arg.Do<ShoppingList>(l => capturedList = l),
            Arg.Any<CancellationToken>());

        var command = CreateValidCommand();

        await handler.HandleAsync(command);

        capturedList.Should().NotBeNull();
        capturedList!.Owner.Value.Should().Be("specific-user-id");
    }

    [Fact]
    public async Task HandleAsync_ForwardsCancellationToken()
    {
        var cts = new CancellationTokenSource();
        var command = CreateValidCommand();

        await handler.HandleAsync(command, cts.Token);

        await shoppingLists.Received(1).AddAsync(
            Arg.Any<ShoppingList>(),
            cts.Token);
    }

    [Fact]
    public async Task HandleAsync_WithRecipeIdentifier_IncludesInDto()
    {
        var recipeId = Guid.NewGuid();
        var command = new CreateShoppingListCommand(
            new ShoppingListTitle("From Recipe"),
            [new CreateShoppingListItemCommand(new ItemName("Flour"), new Amount(500), new Unit("g"))],
            recipeId);

        var result = await handler.HandleAsync(command);

        result.Value.RecipeIdentifier.Should().Be(recipeId);
    }

    [Fact]
    public async Task HandleAsync_ItemsPreserveAmountAndUnit()
    {
        var command = new CreateShoppingListCommand(
            new ShoppingListTitle("Test"),
            [
                new CreateShoppingListItemCommand(new ItemName("Flour"), new Amount(500), new Unit("g")),
                new CreateShoppingListItemCommand(new ItemName("Salt"), null, null),
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
            new ShoppingListTitle("Weekly Groceries"),
            [new CreateShoppingListItemCommand(new ItemName("Spaghetti"), new Amount(400), new Unit("g"))]);
    }
}
