using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Extraction.Services.Tools;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Tests.Services.Tools;

public class GetMergedShoppingListToolTests
{
	private readonly IShoppingListLookup lookup = Substitute.For<IShoppingListLookup>();

	[Fact]
	public async Task GetAsync_DefaultCall_ExcludesPastBought()
	{
		bool capturedFlag = true;
		lookup.GetMergedAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				capturedFlag = callInfo.ArgAt<bool>(0);
				return new ShoppingListLookupResult([]);
			});

		var tool = new GetMergedShoppingListTool(lookup);
		await tool.GetAsync();

		capturedFlag.Should().BeFalse();
	}

	[Fact]
	public async Task GetAsync_ReturnsLookupResultDirectly()
	{
		var expected = new ShoppingListLookupResult([
			new ShoppingListLookupItem("Eggs", 6m, "pcs", "Dairy", IsBought: false),
			new ShoppingListLookupItem("Flour", 500m, "g", "Pantry", IsBought: false),
		]);
		lookup.GetMergedAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(expected);

		var tool = new GetMergedShoppingListTool(lookup);
		var result = await tool.GetAsync();

		result.Should().BeSameAs(expected);
	}

	[Fact]
	public async Task GetAsync_LookupReturnsNull_ReturnsNull()
	{
		lookup.GetMergedAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns((ShoppingListLookupResult?)null);

		var tool = new GetMergedShoppingListTool(lookup);
		var result = await tool.GetAsync();

		result.Should().BeNull();
	}

	[Fact]
	public async Task GetAsync_IncludePastBoughtTrue_ForwardsFlag()
	{
		bool capturedFlag = false;
		lookup.GetMergedAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				capturedFlag = callInfo.ArgAt<bool>(0);
				return new ShoppingListLookupResult([]);
			});

		var tool = new GetMergedShoppingListTool(lookup);
		await tool.GetAsync(includePastBought: true);

		capturedFlag.Should().BeTrue();
	}
}
