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
	private readonly IShoppingListProjectionRepository projection = Substitute.For<IShoppingListProjectionRepository>();
	private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
	private readonly GetShoppingListByIdQueryHandler handler;

	public GetShoppingListByIdQueryHandlerTests()
	{
		currentUser.UserId.Returns("user-123");
		handler = new GetShoppingListByIdQueryHandler(projection, currentUser);
	}

	[Fact]
	public async Task HandleAsync_ExistingList_ReturnsSuccess()
	{
		var listId = ShoppingListIdentifier.New();
		var dto = new ShoppingListDetailDto(listId.Value, "Test List", RecipeReference: null, DateTime.UtcNow, Items: []);
		projection.GetByIdAsync(listId, Arg.Any<CancellationToken>())
			.Returns(new ShoppingListProjectedDetail("user-123", dto));

		var result = await handler.HandleAsync(new GetShoppingListByIdQuery(listId));

		result.IsSuccess.Should().BeTrue();
		result.Value.Title.Should().Be("Test List");
	}

	[Fact]
	public async Task HandleAsync_NotFound_ThrowsEntityNotFoundException()
	{
		var listId = ShoppingListIdentifier.New();
		projection.GetByIdAsync(listId, Arg.Any<CancellationToken>())
			.Returns<ShoppingListProjectedDetail>(_ => throw new EntityNotFoundException(nameof(ShoppingList), listId.Value));

		var act = () => handler.HandleAsync(new GetShoppingListByIdQuery(listId));

		await act.Should().ThrowAsync<EntityNotFoundException>();
	}

	[Fact]
	public async Task HandleAsync_DifferentOwner_ReturnsAccessDenied()
	{
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
		var listId = ShoppingListIdentifier.New();
		var dto = new ShoppingListDetailDto(listId.Value, "T", null, DateTime.UtcNow, []);
		projection.GetByIdAsync(listId, Arg.Any<CancellationToken>())
			.Returns(new ShoppingListProjectedDetail("user-123", dto));
		var cts = new CancellationTokenSource();

		await handler.HandleAsync(new GetShoppingListByIdQuery(listId), cts.Token);

		await projection.Received(1).GetByIdAsync(listId, cts.Token);
	}
}
