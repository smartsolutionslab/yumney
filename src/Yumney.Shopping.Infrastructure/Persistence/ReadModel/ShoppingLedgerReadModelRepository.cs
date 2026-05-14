using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Shared.Quantities;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;

public sealed class ShoppingLedgerReadModelRepository(ShoppingReadDbContext context) : IShoppingLedgerReadModelRepository
{
	public async Task<MergedShoppingListDto> GetByOwnerAsync(
		OwnerIdentifier owner,
		bool includePastBought = false,
		CancellationToken cancellationToken = default)
	{
		var today = DateTime.UtcNow.Date;
		var ownerValue = owner.Value;

		var query = context.ShoppingLedgerReadItems
			.Where(row => row.OwnerId == ownerValue);

		if (!includePastBought)
		{
			query = query.Where(row => !row.IsBought || row.BoughtAt == null || row.BoughtAt >= today);
		}

		var readItems = await query.ToListAsync(cancellationToken);

		var dtoItems = readItems
			.Select(row => row.ToMergedItemDto(DeserializeSources(row.SourcesJson)))
			.OrderBy(item => IngredientCategory.From(item.Category).DisplayOrder)
			.ToList();

		return dtoItems.ToMergedListDto();
	}

	private static List<ItemSourceDto> DeserializeSources(string sourcesJson)
	{
		var sources = JsonSerializer.Deserialize<List<SourceEntry>>(sourcesJson) ?? [];
		return sources.Select(source => new ItemSourceDto(source.Quantity, source.Source, source.OccurredAt)).ToList();
	}

	private sealed record SourceEntry(decimal Quantity, string Source, DateTime OccurredAt);
}
