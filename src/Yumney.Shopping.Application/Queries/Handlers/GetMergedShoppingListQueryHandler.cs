using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Queries.Handlers;

public sealed class GetMergedShoppingListQueryHandler(
    IShoppingLedgerRepository ledgers,
    ICurrentUser currentUser) : IQueryHandler<GetMergedShoppingListQuery, Result<MergedShoppingListDto>>
{
    public async Task<Result<MergedShoppingListDto>> HandleAsync(GetMergedShoppingListQuery query, CancellationToken cancellationToken = default)
    {
        var owner = currentUser.AsOwner();

        var ledger = await ledgers.FindByOwnerAsync(owner, cancellationToken);
        if (ledger is null)
            return new MergedShoppingListDto([]);

        var mergedItems = ledger.GetMergedItems();

        var dtoItems = mergedItems.Select(item =>
        {
            var category = IngredientCategoryResolver.Resolve(item.ItemName) ?? IngredientCategory.Other;
            var sources = item.Sources.Select(s => new ItemSourceDto(s.Quantity, s.Source, s.OccurredAt)).ToList();
            return new MergedShoppingItemDto(item.ItemName, item.TotalQuantity, item.Unit, category.Value, item.IsBought, sources);
        })
        .OrderBy(i => IngredientCategory.From(i.Category).DisplayOrder)
        .ToList();

        return new MergedShoppingListDto(dtoItems);
    }
}
