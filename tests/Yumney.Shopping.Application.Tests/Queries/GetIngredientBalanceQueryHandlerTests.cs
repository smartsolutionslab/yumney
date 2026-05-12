using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shared.Quantities;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shopping.Application.Queries;
using SmartSolutionsLab.Yumney.Shopping.Application.Queries.Handlers;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Tests.Queries;

public class GetIngredientBalanceQueryHandlerTests
{
	private readonly IIngredientBalanceReadModelRepository readModel = Substitute.For<IIngredientBalanceReadModelRepository>();
	private readonly IStaplesProvider staplesProvider = Substitute.For<IStaplesProvider>();
	private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
	private readonly GetIngredientBalanceQueryHandler handler;

	public GetIngredientBalanceQueryHandlerTests()
	{
		currentUser.UserId.Returns("user-123");
		handler = new GetIngredientBalanceQueryHandler(readModel, staplesProvider, currentUser);
	}

	[Fact]
	public async Task HandleAsync_NoAtHomeNoStaples_ReturnsEmptyList()
	{
		ConfigureRepositories(atHome: [], staples: []);

		var result = await handler.HandleAsync(new GetIngredientBalanceQuery());

		result.IsSuccess.Should().BeTrue();
		result.Value.Items.Should().BeEmpty();
	}

	[Fact]
	public async Task HandleAsync_AtHomeItem_ReturnsAsAtHomeSource()
	{
		var atHome = new IngredientBalanceItemDto("Milk", 4m, "L", "dairy", IngredientBalanceSource.AtHome);
		ConfigureRepositories(atHome: [atHome], staples: []);

		var result = await handler.HandleAsync(new GetIngredientBalanceQuery());

		result.Value.Items.Should().ContainSingle()
			.Which.Should().BeEquivalentTo(atHome);
	}

	[Fact]
	public async Task HandleAsync_StapleNotInAtHome_AppearsAsStaple()
	{
		ConfigureRepositories(atHome: [], staples: ["salt", "pepper"]);

		var result = await handler.HandleAsync(new GetIngredientBalanceQuery());

		result.Value.Items.Should().HaveCount(2);
		result.Value.Items.Should().AllSatisfy(i =>
		{
			i.Source.Should().Be(IngredientBalanceSource.Staple);
			i.Quantity.Should().BeNull();
			i.Unit.Should().BeNull();
		});
		result.Value.Items.Select(item => item.ItemName).Should().BeEquivalentTo(["salt", "pepper"]);
	}

	[Fact]
	public async Task HandleAsync_StapleAlreadyAtHome_DoesNotDuplicate()
	{
		var atHome = new IngredientBalanceItemDto("Butter", 250m, "g", "dairy", IngredientBalanceSource.AtHome);
		ConfigureRepositories(atHome: [atHome], staples: ["butter", "salt"]);

		var result = await handler.HandleAsync(new GetIngredientBalanceQuery());

		result.Value.Items.Should().HaveCount(2);
		result.Value.Items.Where(item => string.Equals(item.ItemName, "Butter", StringComparison.OrdinalIgnoreCase))
			.Should().ContainSingle()
			.Which.Source.Should().Be(IngredientBalanceSource.AtHome);
	}

	[Fact]
	public async Task HandleAsync_FetchesByCurrentUser()
	{
		ConfigureRepositories(atHome: [], staples: []);

		await handler.HandleAsync(new GetIngredientBalanceQuery());

		await readModel.Received(1).GetAtHomeItemsAsync(OwnerIdentifier.From("user-123"), Arg.Any<CancellationToken>());
		await staplesProvider.Received(1).GetStapleNamesAsync(Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_OrdersByCategoryThenName()
	{
		var atHome = new[]
		{
			new IngredientBalanceItemDto("Yogurt", 500m, "g", IngredientCategory.Dairy.Value, IngredientBalanceSource.AtHome),
			new IngredientBalanceItemDto("Tomato", 3m, null, IngredientCategory.Produce.Value, IngredientBalanceSource.AtHome),
			new IngredientBalanceItemDto("Apple", 6m, null, IngredientCategory.Produce.Value, IngredientBalanceSource.AtHome),
		};
		ConfigureRepositories(atHome: atHome, staples: []);

		var result = await handler.HandleAsync(new GetIngredientBalanceQuery());

		var names = result.Value.Items.Select(item => item.ItemName).ToList();
		names.Should().Equal("Apple", "Tomato", "Yogurt");
	}

	private void ConfigureRepositories(IReadOnlyList<IngredientBalanceItemDto> atHome, IReadOnlyCollection<string> staples)
	{
		readModel.GetAtHomeItemsAsync(OwnerIdentifier.From("user-123"), Arg.Any<CancellationToken>()).Returns(atHome);
		staplesProvider.GetStapleNamesAsync(Arg.Any<CancellationToken>())
			.Returns(staples.ToHashSet(StringComparer.OrdinalIgnoreCase));
	}
}
