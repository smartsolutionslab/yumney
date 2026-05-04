using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Queries;

public sealed record ExportShoppingListQuery : IQuery<Result<string>>;
