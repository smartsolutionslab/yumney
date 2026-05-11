using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Extraction.Services.Tools;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Tests.Services.Tools;

public class RemoveShoppingItemToolTests
{
	private readonly IShoppingListItemRemover remover = Substitute.For<IShoppingListItemRemover>();

	[Fact]
	public async Task RemoveAsync_HappyPath_DispatchesAndReturnsConfirmation()
	{
		RemoveShoppingItemRequest? captured = null;
		remover.RemoveAsync(Arg.Any<RemoveShoppingItemRequest>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				captured = callInfo.ArgAt<RemoveShoppingItemRequest>(0);
				return true;
			});

		var tool = new RemoveShoppingItemTool(remover);
		var reply = await tool.RemoveAsync("eggs", reason: "bought");

		reply.Should().Contain("Removed eggs");
		captured.Should().NotBeNull();
		captured!.Name.Should().Be("eggs");
		captured.Reason.Should().Be("bought");
	}

	[Fact]
	public async Task RemoveAsync_BlankName_ReturnsErrorWithoutCallingRemover()
	{
		var tool = new RemoveShoppingItemTool(remover);
		var reply = await tool.RemoveAsync(string.Empty);

		reply.Should().Contain("Item name is required");
		await remover.DidNotReceiveWithAnyArgs().RemoveAsync(default!, default);
	}

	[Fact]
	public async Task RemoveAsync_RemoverFails_ReturnsNotFoundMessage()
	{
		remover.RemoveAsync(Arg.Any<RemoveShoppingItemRequest>(), Arg.Any<CancellationToken>()).Returns(false);

		var tool = new RemoveShoppingItemTool(remover);
		var reply = await tool.RemoveAsync("ghost-item");

		reply.Should().Contain("Couldn't find");
	}
}
