using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;
using SmartSolutionsLab.Yumney.MealPlan.Infrastructure.ExternalServices;
using SmartSolutionsLab.Yumney.Shopping.Client;
using Xunit;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Tests.Services;

public class HttpShoppingListWriterTests
{
	[Fact]
	public async Task AddItemsAsync_NoItems_DoesNotCallClient()
	{
		var shopping = Substitute.For<IShoppingClient>();
		var writer = new HttpShoppingListWriter(shopping);

		await writer.AddItemsAsync([]);

		await shopping.DidNotReceiveWithAnyArgs().AddItemAsync(default!, default);
	}

	[Fact]
	public async Task AddItemsAsync_PostsOneRequestPerItem()
	{
		var shopping = Substitute.For<IShoppingClient>();
		var writer = new HttpShoppingListWriter(shopping);

		await writer.AddItemsAsync(
		[
			new ShoppingItemRequest("Tomato", 200m, "g", "meal-plan"),
			new ShoppingItemRequest("Cheese", 100m, "g", "meal-plan"),
		]);

		await shopping.Received(2).AddItemAsync(Arg.Any<AddShoppingItemRequest>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task AddItemsAsync_MapsFieldsToWireRequest()
	{
		var shopping = Substitute.For<IShoppingClient>();
		var writer = new HttpShoppingListWriter(shopping);

		await writer.AddItemsAsync([new ShoppingItemRequest("Pasta", 500m, "g", "meal-plan")]);

		await shopping.Received(1).AddItemAsync(
			Arg.Is<AddShoppingItemRequest>(request =>
				request.Name == "Pasta" &&
				request.Quantity == 500m &&
				request.Unit == "g" &&
				request.Source == "meal-plan"),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task AddItemsAsync_CancelledBeforeStart_ThrowsAndNoItemsPosted()
	{
		var shopping = Substitute.For<IShoppingClient>();
		var writer = new HttpShoppingListWriter(shopping);
		using var cts = new CancellationTokenSource();
		cts.Cancel();

		var act = async () => await writer.AddItemsAsync(
			[new ShoppingItemRequest("Pasta", 500m, "g", "meal-plan")],
			cts.Token);

		await act.Should().ThrowAsync<OperationCanceledException>();
		await shopping.DidNotReceiveWithAnyArgs().AddItemAsync(default!, default);
	}
}
