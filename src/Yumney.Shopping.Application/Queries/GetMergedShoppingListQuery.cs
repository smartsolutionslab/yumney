using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Queries;

public sealed record GetMergedShoppingListQuery(bool IncludePastBought = false) : IQuery<Result<MergedShoppingListDto>>;
