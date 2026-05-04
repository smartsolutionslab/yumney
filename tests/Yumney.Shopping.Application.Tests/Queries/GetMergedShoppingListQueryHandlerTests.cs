using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shopping.Application.Queries;
using SmartSolutionsLab.Yumney.Shopping.Application.Queries.Handlers;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Tests.Queries;

public class GetMergedShoppingListQueryHandlerTests
{
	private readonly IShoppingLedgerReadModelRepository readModel = Substitute.For<IShoppingLedgerReadModelRepository>();
	private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
	private readonly GetMergedShoppingListQueryHandler handler;

	public GetMergedShoppingListQueryHandlerTests()
	{
		currentUser.UserId.Returns("user-123");
		handler = new GetMergedShoppingListQueryHandler(readModel, currentUser);
	}

	[Fact]
	public async Task HandleAsync_EmptyReadModel_ReturnsEmptyList()
	{
		readModel.GetByOwnerAsync(OwnerIdentifier.From("user-123"), Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(new MergedShoppingListDto([]));

		var result = await handler.HandleAsync(new GetMergedShoppingListQuery());

		result.IsSuccess.Should().BeTrue();
		result.Value.Items.Should().BeEmpty();
	}

	[Fact]
	public async Task HandleAsync_WithItems_ReturnsFromReadModel()
	{
		var items = new List<MergedShoppingItemDto>
		{
			new("Milk", 2, 2, "L", "dairy", false, []),
			new("Chicken", 500, 500, "g", "meat-fish", false, []),
		};
		readModel.GetByOwnerAsync(OwnerIdentifier.From("user-123"), Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(new MergedShoppingListDto(items));

		var result = await handler.HandleAsync(new GetMergedShoppingListQuery());

		result.IsSuccess.Should().BeTrue();
		result.Value.Items.Should().HaveCount(2);
	}

	[Fact]
	public async Task HandleAsync_DelegatesToReadModel()
	{
		readModel.GetByOwnerAsync(OwnerIdentifier.From("user-123"), Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(new MergedShoppingListDto([]));

		await handler.HandleAsync(new GetMergedShoppingListQuery());

		await readModel.Received(1).GetByOwnerAsync(OwnerIdentifier.From("user-123"), Arg.Any<bool>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_DefaultQuery_HidesPastBoughtItems()
	{
		readModel.GetByOwnerAsync(OwnerIdentifier.From("user-123"), Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(new MergedShoppingListDto([]));

		await handler.HandleAsync(new GetMergedShoppingListQuery());

		await readModel.Received(1).GetByOwnerAsync(OwnerIdentifier.From("user-123"), false, Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_IncludePastBought_ForwardsToReadModel()
	{
		readModel.GetByOwnerAsync(OwnerIdentifier.From("user-123"), Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(new MergedShoppingListDto([]));

		await handler.HandleAsync(new GetMergedShoppingListQuery(IncludePastBought: true));

		await readModel.Received(1).GetByOwnerAsync(OwnerIdentifier.From("user-123"), true, Arg.Any<CancellationToken>());
	}
}
