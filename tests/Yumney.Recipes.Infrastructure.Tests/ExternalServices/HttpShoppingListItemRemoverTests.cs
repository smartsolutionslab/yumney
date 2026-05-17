using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure.ExternalServices;
using SmartSolutionsLab.Yumney.Shopping.Client;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Tests.ExternalServices;

public class HttpShoppingListItemRemoverTests
{
	private readonly IShoppingClient client = Substitute.For<IShoppingClient>();

	[Fact]
	public async Task RemoveAsync_MapsConsumerRequestToClientBody()
	{
		RemoveShoppingItemBody? captured = null;
		client.RemoveItemAsync(Arg.Any<RemoveShoppingItemBody>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				captured = callInfo.ArgAt<RemoveShoppingItemBody>(0);
				return true;
			});

		var remover = new HttpShoppingListItemRemover(client);
		var ok = await remover.RemoveAsync(new RemoveShoppingItemRequest("Onion", 1m, "kg", "out of stock"));

		ok.Should().BeTrue();
		captured.Should().NotBeNull();
		captured!.Name.Should().Be("Onion");
		captured.Quantity.Should().Be(1m);
		captured.Unit.Should().Be("kg");
		captured.Reason.Should().Be("out of stock");
	}

	[Fact]
	public async Task RemoveAsync_ClientReturnsFalse_PropagatesFalse()
	{
		client.RemoveItemAsync(Arg.Any<RemoveShoppingItemBody>(), Arg.Any<CancellationToken>())
			.Returns(false);

		var remover = new HttpShoppingListItemRemover(client);
		var ok = await remover.RemoveAsync(new RemoveShoppingItemRequest("Salt", null, null, null));

		ok.Should().BeFalse();
	}
}
