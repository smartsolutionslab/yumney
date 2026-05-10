using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure.ExternalServices;
using SmartSolutionsLab.Yumney.Shopping.Client;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Tests.ExternalServices;

public class HttpShoppingListLookupTests
{
	private readonly IShoppingClient client = Substitute.For<IShoppingClient>();

	[Fact]
	public async Task GetMergedAsync_ClientReturnsResponse_MapsToLookupResult()
	{
		client.GetMergedListAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(new MergedShoppingListResponse([
				new MergedShoppingListItemResponse("Eggs", 6m, "pcs", "Dairy", IsBought: false),
				new MergedShoppingListItemResponse("Flour", 500m, "g", "Pantry", IsBought: true),
			]));

		var lookup = new HttpShoppingListLookup(client);
		var result = await lookup.GetMergedAsync();

		result.Should().NotBeNull();
		result!.Items.Should().HaveCount(2);
		result.Items[0].Name.Should().Be("Eggs");
		result.Items[0].Quantity.Should().Be(6m);
		result.Items[0].Unit.Should().Be("pcs");
		result.Items[0].Category.Should().Be("Dairy");
		result.Items[0].IsBought.Should().BeFalse();
		result.Items[1].IsBought.Should().BeTrue();
	}

	[Fact]
	public async Task GetMergedAsync_ClientReturnsNull_ReturnsNull()
	{
		client.GetMergedListAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns((MergedShoppingListResponse?)null);

		var lookup = new HttpShoppingListLookup(client);
		var result = await lookup.GetMergedAsync();

		result.Should().BeNull();
	}

	[Fact]
	public async Task GetMergedAsync_ForwardsIncludePastBoughtFlag()
	{
		client.GetMergedListAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns((MergedShoppingListResponse?)null);

		var lookup = new HttpShoppingListLookup(client);
		await lookup.GetMergedAsync(includePastBought: true);

		await client.Received(1).GetMergedListAsync(true, Arg.Any<CancellationToken>());
	}
}
