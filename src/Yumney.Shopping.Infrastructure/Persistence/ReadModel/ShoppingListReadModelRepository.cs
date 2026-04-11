using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.ReadModel;

public sealed class ShoppingListReadModelRepository(ShoppingDbContext context) : IShoppingListReadModelRepository
{
    public async Task<MergedShoppingListDto> GetByOwnerAsync(string ownerId, CancellationToken cancellationToken = default)
    {
        var readItems = await context.ShoppingListReadItems
            .AsNoTracking()
            .Where(r => r.OwnerId == ownerId)
            .ToListAsync(cancellationToken);

        var dtoItems = readItems
            .Select(r =>
            {
                var sources = JsonSerializer.Deserialize<List<SourceEntry>>(r.SourcesJson) ?? [];
                var sourceDtos = sources.Select(s => new ItemSourceDto(s.Quantity, s.Source, s.OccurredAt)).ToList();
                var rounded = QuantityRounder.RoundUp(r.TotalQuantity, r.Unit);
                return new MergedShoppingItemDto(r.ItemName, r.TotalQuantity, rounded.DisplayQuantity, r.Unit, r.Category, r.IsBought, sourceDtos);
            })
            .OrderBy(i => IngredientCategory.From(i.Category).DisplayOrder)
            .ToList();

        return new MergedShoppingListDto(dtoItems);
    }

    private sealed record SourceEntry(decimal Quantity, string Source, DateTime OccurredAt);
}
