using System.Globalization;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using SmartSolutionsLab.Yumney.Shared.Quantities;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Tests.Persistence.ReadModel;

public class IngredientBalanceReadModelRepositoryTests
{
	private const string OwnerId = "user-1";

	private readonly FakeTimeProvider timeProvider = new(DateTimeOffset.Parse("2026-04-30T12:00:00Z", CultureInfo.InvariantCulture));

	[Fact]
	public async Task GetAtHomeItemsAsync_NoRows_ReturnsEmpty()
	{
		await using var context = CreateContext();
		var repo = new IngredientBalanceReadModelRepository(context, timeProvider);

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

		var repo = new IngredientBalanceReadModelRepository(context, timeProvider);
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
			LastBoughtAt = timeProvider.GetUtcNow().UtcDateTime,
		});
		await context.SaveChangesAsync();

		var repo = new IngredientBalanceReadModelRepository(context, timeProvider);
		var items = await repo.GetAtHomeItemsAsync(OwnerId);

		var item = items.Should().ContainSingle().Subject;
		item.ItemName.Should().Be("Milk");
		item.Quantity.Should().Be(4m);
		item.Unit.Should().Be("l");
		item.Source.Should().Be(IngredientBalanceSource.AtHome);
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

		var repo = new IngredientBalanceReadModelRepository(context, timeProvider);
		var items = await repo.GetAtHomeItemsAsync(OwnerId);

		items.Should().ContainSingle().Which.ItemName.Should().Be("Milk");
	}

	[Fact]
	public async Task GetAtHomeItemsAsync_NoLastBoughtAt_FreshnessIsNotTracked()
	{
		await using var context = CreateContext();
		context.Set<IngredientBalanceReadItem>().Add(MakeRow(IngredientCategory.MeatFish, lastBoughtAt: null));
		await context.SaveChangesAsync();

		var repo = new IngredientBalanceReadModelRepository(context, timeProvider);
		var item = (await repo.GetAtHomeItemsAsync(OwnerId)).Single();

		item.Freshness.Should().Be(Freshness.NotTracked);
		item.DaysSinceBought.Should().BeNull();
	}

	[Fact]
	public async Task GetAtHomeItemsAsync_PantryItem_AlwaysNotTracked()
	{
		await using var context = CreateContext();
		var twentyDaysAgo = timeProvider.GetUtcNow().UtcDateTime.AddDays(-20);
		context.Set<IngredientBalanceReadItem>().Add(MakeRow(IngredientCategory.Pantry, lastBoughtAt: twentyDaysAgo));
		await context.SaveChangesAsync();

		var repo = new IngredientBalanceReadModelRepository(context, timeProvider);
		var item = (await repo.GetAtHomeItemsAsync(OwnerId)).Single();

		item.Freshness.Should().Be(Freshness.NotTracked);
	}

	[Fact]
	public async Task GetAtHomeItemsAsync_FreshMeatBoughtToday_IsFresh()
	{
		await using var context = CreateContext();
		context.Set<IngredientBalanceReadItem>().Add(MakeRow(IngredientCategory.MeatFish, lastBoughtAt: timeProvider.GetUtcNow().UtcDateTime));
		await context.SaveChangesAsync();

		var repo = new IngredientBalanceReadModelRepository(context, timeProvider);
		var item = (await repo.GetAtHomeItemsAsync(OwnerId)).Single();

		item.Freshness.Should().Be(Freshness.Fresh);
		item.DaysSinceBought.Should().Be(0);
	}

	[Fact]
	public async Task GetAtHomeItemsAsync_MeatBoughtOneDayAgo_IsUseSoon()
	{
		await using var context = CreateContext();
		var oneDayAgo = timeProvider.GetUtcNow().UtcDateTime.AddDays(-1);
		context.Set<IngredientBalanceReadItem>().Add(MakeRow(IngredientCategory.MeatFish, lastBoughtAt: oneDayAgo));
		await context.SaveChangesAsync();

		var repo = new IngredientBalanceReadModelRepository(context, timeProvider);
		var item = (await repo.GetAtHomeItemsAsync(OwnerId)).Single();

		item.Freshness.Should().Be(Freshness.UseSoon);
		item.DaysSinceBought.Should().Be(1);
	}

	[Fact]
	public async Task GetAtHomeItemsAsync_MeatBoughtThreeDaysAgo_IsCheckIt()
	{
		await using var context = CreateContext();
		var threeDaysAgo = timeProvider.GetUtcNow().UtcDateTime.AddDays(-3);
		context.Set<IngredientBalanceReadItem>().Add(MakeRow(IngredientCategory.MeatFish, lastBoughtAt: threeDaysAgo));
		await context.SaveChangesAsync();

		var repo = new IngredientBalanceReadModelRepository(context, timeProvider);
		var item = (await repo.GetAtHomeItemsAsync(OwnerId)).Single();

		item.Freshness.Should().Be(Freshness.CheckIt);
		item.DaysSinceBought.Should().Be(3);
	}

	private static IngredientBalanceReadItem MakeRow(IngredientCategory category, DateTime? lastBoughtAt) => new()
	{
		Id = Guid.NewGuid(),
		OwnerId = OwnerId,
		ItemName = "Item",
		NameKey = "item",
		Unit = null,
		Category = category.Value,
		BoughtTotal = 1,
		LastBoughtAt = lastBoughtAt,
	};

	private static ShoppingReadDbContext CreateContext()
	{
		var options = new DbContextOptionsBuilder<ShoppingReadDbContext>()
			.UseInMemoryDatabase($"ingredient-balance-{Guid.NewGuid()}")
			.Options;
		return new ShoppingReadDbContext(options);
	}
}
