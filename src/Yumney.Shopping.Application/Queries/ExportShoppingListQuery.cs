using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Queries;

public sealed record ExportShoppingListQuery : IQuery<Result<string>>;
