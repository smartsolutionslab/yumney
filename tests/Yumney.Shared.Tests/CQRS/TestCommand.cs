using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.Shared.Tests.CQRS;

public sealed record TestCommand : ICommand<Result<string>>;
