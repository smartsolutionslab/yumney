using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure.ExternalServices;
using SmartSolutionsLab.Yumney.Shopping.Client;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Tests.ExternalServices;

public class HttpShoppingListCreatorTests
{
	private readonly IShoppingClient client = Substitute.For<IShoppingClient>();

	[Fact]
	public async Task CreateAsync_MapsConsumerRecipesToClientBody()
	{
		var first = Guid.NewGuid();
		var second = Guid.NewGuid();
		CreateListFromRecipesBody? captured = null;
		client.CreateListFromRecipesAsync(Arg.Any<CreateListFromRecipesBody>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				captured = callInfo.ArgAt<CreateListFromRecipesBody>(0);
				return true;
			});

		var creator = new HttpShoppingListCreator(client);
		await creator.CreateAsync(new CreateShoppingListRequest(
			"This week",
			[
				new CreateShoppingListRecipe(first, Servings: 4),
				new CreateShoppingListRecipe(second, Servings: null),
			]));

		captured.Should().NotBeNull();
		captured!.Title.Should().Be("This week");
		captured.Recipes.Should().HaveCount(2);
		captured.Recipes[0].RecipeIdentifier.Should().Be(first);
		captured.Recipes[0].Servings.Should().Be(4);
		captured.Recipes[1].RecipeIdentifier.Should().Be(second);
		captured.Recipes[1].Servings.Should().BeNull();
	}

	[Fact]
	public async Task CreateAsync_ClientReturnsFalse_PropagatesFalse()
	{
		client.CreateListFromRecipesAsync(Arg.Any<CreateListFromRecipesBody>(), Arg.Any<CancellationToken>())
			.Returns(false);

		var creator = new HttpShoppingListCreator(client);
		var result = await creator.CreateAsync(new CreateShoppingListRequest("List", [new(Guid.NewGuid(), Servings: null)]));

		result.Should().BeFalse();
	}
}
