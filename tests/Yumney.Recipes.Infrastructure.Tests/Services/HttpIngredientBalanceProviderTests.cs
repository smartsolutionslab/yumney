using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure.ExternalServices;
using SmartSolutionsLab.Yumney.Shared.Quantities;
using SmartSolutionsLab.Yumney.Shopping.Client;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Tests.Services;

public class HttpIngredientBalanceProviderTests
{
	[Fact]
	public async Task GetAvailableIngredientsAsync_NullResponse_ReturnsEmptyDictionary()
	{
		var provider = CreateProvider(balance: null);

		var result = await provider.GetAvailableIngredientsAsync();

		result.Should().BeEmpty();
	}

	[Fact]
	public async Task GetAvailableIngredientsAsync_EmptyItems_ReturnsEmptyDictionary()
	{
		var provider = CreateProvider(new ShoppingBalanceResponse([]));

		var result = await provider.GetAvailableIngredientsAsync();

		result.Should().BeEmpty();
	}

	[Fact]
	public async Task GetAvailableIngredientsAsync_LookupIsCaseInsensitive()
	{
		var provider = CreateProvider(new ShoppingBalanceResponse([
			new ShoppingBalanceItem("Milk", Freshness.Fresh),
		]));

		var result = await provider.GetAvailableIngredientsAsync();

		result.ContainsKey("milk").Should().BeTrue();
		result.ContainsKey("MILK").Should().BeTrue();
	}

	[Fact]
	public async Task GetAvailableIngredientsAsync_TrimsWhitespaceFromNames()
	{
		var provider = CreateProvider(new ShoppingBalanceResponse([
			new ShoppingBalanceItem("  Milk  ", Freshness.Fresh),
		]));

		var result = await provider.GetAvailableIngredientsAsync();

		result.Should().ContainSingle().Which.Key.Should().Be("Milk");
	}

	[Fact]
	public async Task GetAvailableIngredientsAsync_BlankName_Skipped()
	{
		var provider = CreateProvider(new ShoppingBalanceResponse([
			new ShoppingBalanceItem("   ", Freshness.Fresh),
			new ShoppingBalanceItem("Eggs", Freshness.Fresh),
		]));

		var result = await provider.GetAvailableIngredientsAsync();

		result.Should().ContainSingle().Which.Key.Should().Be("Eggs");
	}

	[Fact]
	public async Task GetAvailableIngredientsAsync_DuplicateNameMostUrgentWins()
	{
		var provider = CreateProvider(new ShoppingBalanceResponse([
			new ShoppingBalanceItem("Milk", Freshness.Fresh),
			new ShoppingBalanceItem("milk", Freshness.UseSoon),
			new ShoppingBalanceItem("MILK", Freshness.NotTracked),
		]));

		var result = await provider.GetAvailableIngredientsAsync();

		result.Should().ContainSingle().Which.Value.Should().Be(Freshness.UseSoon);
	}

	[Fact]
	public async Task GetAvailableIngredientsAsync_DuplicateNameOrderIndependent()
	{
		var provider = CreateProvider(new ShoppingBalanceResponse([
			new ShoppingBalanceItem("Chicken", Freshness.CheckIt),
			new ShoppingBalanceItem("Chicken", Freshness.Fresh),
		]));

		var result = await provider.GetAvailableIngredientsAsync();

		result["Chicken"].Should().Be(Freshness.CheckIt);
	}

	private static HttpIngredientBalanceProvider CreateProvider(ShoppingBalanceResponse? balance)
	{
		var shopping = Substitute.For<IShoppingClient>();
		shopping.GetBalanceAsync(Arg.Any<CancellationToken>()).Returns(balance);
		return new HttpIngredientBalanceProvider(shopping);
	}
}
