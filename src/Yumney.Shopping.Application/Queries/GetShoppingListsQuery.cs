using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Queries;

public sealed record GetShoppingListsQuery(
    PagingOptions Paging,
    SortingOptions<ShoppingListSortField> Sorting) : IQuery<Result<PagedResult<ShoppingListSummaryDto>>>;
