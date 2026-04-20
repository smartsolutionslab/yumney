using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Queries;

public sealed record GetMergedShoppingListQuery(bool IncludePastBought = false) : IQuery<Result<MergedShoppingListDto>>;
