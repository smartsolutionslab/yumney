using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure.ExternalServices;
using Xunit;
using ShoppingClient = SmartSolutionsLab.Yumney.Shopping.Client;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Tests.ExternalServices;

public class HttpShoppingListItemAdderTests
{
	private readonly ShoppingClient.IShoppingClient client = Substitute.For<ShoppingClient.IShoppingClient>();

	[Fact]
	public async Task AddAsync_MapsConsumerRequestToClientBody_AndStampsChatSource()
	{
		ShoppingClient.AddShoppingItemRequest? captured = null;
		await client.AddItemAsync(
			Arg.Do<ShoppingClient.AddShoppingItemRequest>(body => captured = body),
			Arg.Any<CancellationToken>());

		var adder = new HttpShoppingListItemAdder(client);
		var ok = await adder.AddAsync(new AddShoppingItemRequest("Onion", 2m, "kg"));

		ok.Should().BeTrue();
		captured.Should().NotBeNull();
		captured!.Name.Should().Be("Onion");
		captured.Quantity.Should().Be(2m);
		captured.Unit.Should().Be("kg");
		captured.Source.Should().Be("chat");
	}

	[Fact]
	public async Task AddAsync_QuantityNull_DefaultsToOne()
	{
		ShoppingClient.AddShoppingItemRequest? captured = null;
		await client.AddItemAsync(
			Arg.Do<ShoppingClient.AddShoppingItemRequest>(body => captured = body),
			Arg.Any<CancellationToken>());

		var adder = new HttpShoppingListItemAdder(client);
		await adder.AddAsync(new AddShoppingItemRequest("Salt", Quantity: null, Unit: null));

		captured!.Quantity.Should().Be(1m);
	}

	[Fact]
	public async Task AddAsync_ClientThrows_ReturnsFalse()
	{
		client.AddItemAsync(Arg.Any<ShoppingClient.AddShoppingItemRequest>(), Arg.Any<CancellationToken>())
			.Throws(new InvalidOperationException("boom"));

		var adder = new HttpShoppingListItemAdder(client);
		var ok = await adder.AddAsync(new AddShoppingItemRequest("Sugar", 1m, null));

		ok.Should().BeFalse();
	}

	[Fact]
	public async Task AddAsync_ClientThrowsOperationCanceled_Rethrows()
	{
		client.AddItemAsync(Arg.Any<ShoppingClient.AddShoppingItemRequest>(), Arg.Any<CancellationToken>())
			.Throws(new OperationCanceledException());

		var adder = new HttpShoppingListItemAdder(client);
		var act = async () => await adder.AddAsync(new AddShoppingItemRequest("Sugar", 1m, null));

		await act.Should().ThrowAsync<OperationCanceledException>();
	}
}
