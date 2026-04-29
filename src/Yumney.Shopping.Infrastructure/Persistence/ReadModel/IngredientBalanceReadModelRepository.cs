using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;

public sealed class IngredientBalanceReadModelRepository(ShoppingReadDbContext context) : IIngredientBalanceReadModelRepository
{
	public async Task<IReadOnlyList<IngredientBalanceItemDto>> GetAtHomeItemsAsync(string ownerId, CancellationToken cancellationToken = default)
	{
		var rows = await context.IngredientBalanceReadItems
			.Where(r => r.OwnerId == ownerId)
			.ToListAsync(cancellationToken);

		return rows
			.Where(r => r.AtHome > 0)
			.Select(r => new IngredientBalanceItemDto(
				ItemName: r.ItemName,
				Quantity: r.AtHome,
				Unit: r.Unit,
				Category: r.Category,
				Source: IngredientBalanceSource.AtHome))
			.ToList();
	}
}
