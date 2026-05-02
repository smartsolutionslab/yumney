using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Shared.Tests.CQRS;

public sealed record PlainCommand : ICommand<string>;
