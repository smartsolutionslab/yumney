using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;

public sealed class IngredientBalanceReadModelRepository(ShoppingReadDbContext context, TimeProvider timeProvider) : IIngredientBalanceReadModelRepository
{
	public async Task<IReadOnlyList<IngredientBalanceItemDto>> GetAtHomeItemsAsync(string ownerId, CancellationToken cancellationToken = default)
	{
		var rows = await context.IngredientBalanceReadItems
			.Where(r => r.OwnerId == ownerId)
			.ToListAsync(cancellationToken);

		var now = timeProvider.GetUtcNow().UtcDateTime;

		return rows
			.Where(r => r.AtHome > 0)
			.Select(r =>
			{
				var category = IngredientCategory.From(r.Category);
				var daysSinceBought = r.LastBoughtAt is null
					? (int?)null
					: Math.Max(0, (int)(now - r.LastBoughtAt.Value).TotalDays);
				var freshness = ShelfLife.Classify(category, daysSinceBought);

				return new IngredientBalanceItemDto(
					ItemName: r.ItemName,
					Quantity: r.AtHome,
					Unit: r.Unit,
					Category: r.Category,
					Source: IngredientBalanceSource.AtHome,
					Freshness: freshness,
					DaysSinceBought: daysSinceBought);
			})
			.ToList();
	}
}
