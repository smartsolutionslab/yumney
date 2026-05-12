using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Shared.Quantities;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;

public sealed class IngredientBalanceReadModelRepository(ShoppingReadDbContext context, TimeProvider timeProvider) : IIngredientBalanceReadModelRepository
{
	public async Task<IReadOnlyList<IngredientBalanceItemDto>> GetAtHomeItemsAsync(
		OwnerIdentifier owner,
		CancellationToken cancellationToken = default)
	{
		var ownerValue = owner.Value;

		var rows = await context.IngredientBalanceReadItems
			.Where(row => row.OwnerId == ownerValue)
			.ToListAsync(cancellationToken);

		var now = timeProvider.GetUtcNow().UtcDateTime;

		return rows
			.Where(row => row.AtHome > 0)
			.Select(row =>
			{
				var category = IngredientCategory.From(row.Category);
				var daysSinceBought = row.LastBoughtAt is null
					? (int?)null
					: Math.Max(0, (int)(now - row.LastBoughtAt.Value).TotalDays);
				var freshness = ShelfLife.Classify(category, daysSinceBought);
				return row.ToDto(freshness, daysSinceBought);
			})
			.ToList();
	}
}
