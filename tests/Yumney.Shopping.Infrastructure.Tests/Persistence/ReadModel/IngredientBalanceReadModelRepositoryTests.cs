using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Tests.Persistence.ReadModel;

public class IngredientBalanceReadModelRepositoryTests
{
	private const string OwnerId = "user-1";

	[Fact]
	public async Task GetAtHomeItemsAsync_NoRows_ReturnsEmpty()
	{
		await using var context = CreateContext();
		var repo = new IngredientBalanceReadModelRepository(context);

		var items = await repo.GetAtHomeItemsAsync(OwnerId);

		items.Should().BeEmpty();
	}

	[Fact]
	public async Task GetAtHomeItemsAsync_OnlyZeroBalanceRow_ExcludesIt()
	{
		await using var context = CreateContext();
		context.Set<IngredientBalanceReadItem>().Add(new IngredientBalanceReadItem
		{
			Id = Guid.NewGuid(),
			OwnerId = OwnerId,
			ItemName = "Milk",
			NameKey = "milk",
			Unit = "l",
			Category = IngredientCategory.Dairy.Value,
			BoughtTotal = 2,
			ConsumedTotal = 2,
			RemovedTotal = 0,
		});
		await context.SaveChangesAsync();

		var repo = new IngredientBalanceReadModelRepository(context);
		var items = await repo.GetAtHomeItemsAsync(OwnerId);

		items.Should().BeEmpty();
	}

	[Fact]
	public async Task GetAtHomeItemsAsync_PositiveBalance_ReturnsAtHomeAmount()
	{
		await using var context = CreateContext();
		context.Set<IngredientBalanceReadItem>().Add(new IngredientBalanceReadItem
		{
			Id = Guid.NewGuid(),
			OwnerId = OwnerId,
			ItemName = "Milk",
			NameKey = "milk",
			Unit = "l",
			Category = IngredientCategory.Dairy.Value,
			BoughtTotal = 5,
			ConsumedTotal = 1,
			RemovedTotal = 0,
		});
		await context.SaveChangesAsync();

		var repo = new IngredientBalanceReadModelRepository(context);
		var items = await repo.GetAtHomeItemsAsync(OwnerId);

		items.Should().ContainSingle()
			.Which.Should().BeEquivalentTo(new IngredientBalanceItemDto(
				ItemName: "Milk",
				Quantity: 4m,
				Unit: "l",
				Category: IngredientCategory.Dairy.Value,
				Source: IngredientBalanceSource.AtHome));
	}

	[Fact]
	public async Task GetAtHomeItemsAsync_FiltersByOwner()
	{
		await using var context = CreateContext();
		context.Set<IngredientBalanceReadItem>().AddRange(
			new IngredientBalanceReadItem
			{
				Id = Guid.NewGuid(),
				OwnerId = OwnerId,
				ItemName = "Milk",
				NameKey = "milk",
				Unit = "l",
				Category = IngredientCategory.Dairy.Value,
				BoughtTotal = 2,
			},
			new IngredientBalanceReadItem
			{
				Id = Guid.NewGuid(),
				OwnerId = "other-user",
				ItemName = "Bread",
				NameKey = "bread",
				Unit = null,
				Category = IngredientCategory.Bakery.Value,
				BoughtTotal = 1,
			});
		await context.SaveChangesAsync();

		var repo = new IngredientBalanceReadModelRepository(context);
		var items = await repo.GetAtHomeItemsAsync(OwnerId);

		items.Should().ContainSingle().Which.ItemName.Should().Be("Milk");
	}

	private static ShoppingReadDbContext CreateContext()
	{
		var options = new DbContextOptionsBuilder<ShoppingReadDbContext>()
			.UseInMemoryDatabase($"ingredient-balance-{Guid.NewGuid()}")
			.Options;
		return new ShoppingReadDbContext(options);
	}
}
