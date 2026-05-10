using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Extraction.Services.Tools;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Tests.Services.Tools;

public class CreateShoppingListToolTests
{
	private readonly IShoppingListCreator creator = Substitute.For<IShoppingListCreator>();

	[Fact]
	public async Task CreateAsync_HappyPath_BuildsRequestAndReturnsConfirmation()
	{
		var first = Guid.NewGuid();
		var second = Guid.NewGuid();
		CreateShoppingListRequest? captured = null;
		creator.CreateAsync(Arg.Any<CreateShoppingListRequest>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				captured = callInfo.ArgAt<CreateShoppingListRequest>(0);
				return true;
			});

		var tool = new CreateShoppingListTool(creator);
		var reply = await tool.CreateAsync("This week's dinners", $"{first}, {second}");

		reply.Should().Contain("This week's dinners");
		reply.Should().Contain("2");
		captured.Should().NotBeNull();
		captured!.Title.Should().Be("This week's dinners");
		captured.Recipes.Should().HaveCount(2);
		captured.Recipes.Select(recipe => recipe.RecipeIdentifier).Should().ContainInOrder(first, second);
		captured.Recipes.Should().AllSatisfy(recipe => recipe.Servings.Should().BeNull());
	}

	[Fact]
	public async Task CreateAsync_NoValidGuids_ReturnsErrorWithoutCallingCreator()
	{
		var tool = new CreateShoppingListTool(creator);
		var reply = await tool.CreateAsync("Foo", "not-a-guid, also-not-one");

		reply.Should().Contain("Need at least one valid recipe identifier");
		await creator.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
	}

	[Fact]
	public async Task CreateAsync_MixedValidAndInvalid_KeepsOnlyValid()
	{
		var valid = Guid.NewGuid();
		CreateShoppingListRequest? captured = null;
		creator.CreateAsync(Arg.Any<CreateShoppingListRequest>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				captured = callInfo.ArgAt<CreateShoppingListRequest>(0);
				return true;
			});

		var tool = new CreateShoppingListTool(creator);
		await tool.CreateAsync("Mix", $"not-a-guid, {valid}, also-not-one");

		captured.Should().NotBeNull();
		captured!.Recipes.Should().ContainSingle();
		captured.Recipes[0].RecipeIdentifier.Should().Be(valid);
	}

	[Fact]
	public async Task CreateAsync_CreatorReturnsFalse_ReturnsErrorMessage()
	{
		creator.CreateAsync(Arg.Any<CreateShoppingListRequest>(), Arg.Any<CancellationToken>())
			.Returns(false);

		var tool = new CreateShoppingListTool(creator);
		var reply = await tool.CreateAsync("List", Guid.NewGuid().ToString());

		reply.Should().Contain("Couldn't create");
	}
}
