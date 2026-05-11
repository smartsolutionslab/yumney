using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Extraction.Services.Tools;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Tests.Services.Tools;

public class AddShoppingItemToolTests
{
	private readonly IShoppingListItemAdder adder = Substitute.For<IShoppingListItemAdder>();

	[Fact]
	public async Task AddAsync_HappyPath_DispatchesAndReturnsConfirmation()
	{
		AddShoppingItemRequest? captured = null;
		adder.AddAsync(Arg.Any<AddShoppingItemRequest>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				captured = callInfo.ArgAt<AddShoppingItemRequest>(0);
				return true;
			});

		var tool = new AddShoppingItemTool(adder);
		var reply = await tool.AddAsync("milk", quantity: 2m, unit: "L");

		reply.Should().Contain("milk");
		reply.Should().Contain("2");
		captured.Should().NotBeNull();
		captured!.Name.Should().Be("milk");
		captured.Quantity.Should().Be(2m);
		captured.Unit.Should().Be("L");
	}

	[Fact]
	public async Task AddAsync_NoQuantityOrUnit_StillAdds()
	{
		AddShoppingItemRequest? captured = null;
		adder.AddAsync(Arg.Any<AddShoppingItemRequest>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				captured = callInfo.ArgAt<AddShoppingItemRequest>(0);
				return true;
			});

		var tool = new AddShoppingItemTool(adder);
		await tool.AddAsync("eggs");

		captured.Should().NotBeNull();
		captured!.Name.Should().Be("eggs");
		captured.Quantity.Should().BeNull();
		captured.Unit.Should().BeNull();
	}

	[Fact]
	public async Task AddAsync_BlankName_ReturnsErrorWithoutCallingAdder()
	{
		var tool = new AddShoppingItemTool(adder);
		var reply = await tool.AddAsync("   ");

		reply.Should().Contain("Item name is required");
		await adder.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
	}

	[Fact]
	public async Task AddAsync_AdderFails_ReturnsErrorMessage()
	{
		adder.AddAsync(Arg.Any<AddShoppingItemRequest>(), Arg.Any<CancellationToken>()).Returns(false);

		var tool = new AddShoppingItemTool(adder);
		var reply = await tool.AddAsync("milk");

		reply.Should().Contain("Couldn't add");
	}
}
