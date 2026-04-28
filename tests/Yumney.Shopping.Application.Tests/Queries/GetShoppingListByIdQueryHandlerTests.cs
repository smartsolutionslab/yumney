using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shopping.Application.Queries;
using SmartSolutionsLab.Yumney.Shopping.Application.Queries.Handlers;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Tests.Queries;

public class GetShoppingListByIdQueryHandlerTests
{
	private readonly IShoppingListRepository shoppingLists = Substitute.For<IShoppingListRepository>();
	private readonly IShoppingListProjectionRepository projection = Substitute.For<IShoppingListProjectionRepository>();
	private readonly ShoppingOptions options = new() { UseProjectionReadModel = false };
	private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
	private readonly GetShoppingListByIdQueryHandler handler;

	public GetShoppingListByIdQueryHandlerTests()
	{
		currentUser.UserId.Returns("user-123");
		handler = new GetShoppingListByIdQueryHandler(shoppingLists, projection, options, currentUser);
	}

	[Fact]
	public async Task HandleAsync_ExistingList_ReturnsSuccess()
	{
		var shoppingList = ShoppingListTestData.CreateList();
		shoppingLists.GetByIdAsync(Arg.Any<ShoppingListIdentifier>(), Arg.Any<CancellationToken>())
			.Returns(shoppingList);

		var query = new GetShoppingListByIdQuery(shoppingList.Identifier);

		var result = await handler.HandleAsync(query);

		result.IsSuccess.Should().BeTrue();
		result.Value.Title.Should().Be("Test List");
	}

	[Fact]
	public async Task HandleAsync_NotFound_ThrowsEntityNotFoundException()
	{
		var listId = ShoppingListIdentifier.New();
		shoppingLists.GetByIdAsync(listId, Arg.Any<CancellationToken>())
			.Returns<ShoppingList>(_ => throw new EntityNotFoundException(nameof(ShoppingList), listId.Value));

		var query = new GetShoppingListByIdQuery(listId);

		var act = () => handler.HandleAsync(query);

		await act.Should().ThrowAsync<EntityNotFoundException>();
	}

	[Fact]
	public async Task HandleAsync_DifferentOwner_ReturnsAccessDenied()
	{
		currentUser.UserId.Returns("different-user");
		var shoppingList = ShoppingListTestData.CreateList();
		shoppingLists.GetByIdAsync(Arg.Any<ShoppingListIdentifier>(), Arg.Any<CancellationToken>())
			.Returns(shoppingList);

		var query = new GetShoppingListByIdQuery(shoppingList.Identifier);

		var result = await handler.HandleAsync(query);

		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(GetShoppingListByIdErrors.AccessDenied);
	}

	[Fact]
	public async Task HandleAsync_WhenProjectionFlagIsOn_QueriesProjectionRepository()
	{
		options.UseProjectionReadModel = true;
		var listId = ShoppingListIdentifier.New();
		var dto = new ShoppingListDetailDto(
			listId.Value,
			"Projected",
			RecipeReference: null,
			DateTime.UtcNow,
			Items: []);
		projection.GetByIdAsync(listId, Arg.Any<CancellationToken>())
			.Returns(new ShoppingListProjectedDetail("user-123", dto));

		var result = await handler.HandleAsync(new GetShoppingListByIdQuery(listId));

		result.IsSuccess.Should().BeTrue();
		result.Value.Title.Should().Be("Projected");
		await shoppingLists.DidNotReceiveWithAnyArgs().GetByIdAsync(default!, default);
	}

	[Fact]
	public async Task HandleAsync_ProjectionDifferentOwner_ReturnsAccessDenied()
	{
		options.UseProjectionReadModel = true;
		var listId = ShoppingListIdentifier.New();
		var dto = new ShoppingListDetailDto(listId.Value, "T", null, DateTime.UtcNow, []);
		projection.GetByIdAsync(listId, Arg.Any<CancellationToken>())
			.Returns(new ShoppingListProjectedDetail("other-user", dto));

		var result = await handler.HandleAsync(new GetShoppingListByIdQuery(listId));

		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(GetShoppingListByIdErrors.AccessDenied);
	}

	[Fact]
	public async Task HandleAsync_ForwardsCancellationToken()
	{
		var shoppingList = ShoppingListTestData.CreateList();
		shoppingLists.GetByIdAsync(Arg.Any<ShoppingListIdentifier>(), Arg.Any<CancellationToken>())
			.Returns(shoppingList);
		var cts = new CancellationTokenSource();
		var query = new GetShoppingListByIdQuery(shoppingList.Identifier);

		await handler.HandleAsync(query, cts.Token);

		await shoppingLists.Received(1).GetByIdAsync(Arg.Any<ShoppingListIdentifier>(), cts.Token);
	}
}
