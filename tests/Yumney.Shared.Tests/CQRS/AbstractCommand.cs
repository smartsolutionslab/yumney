using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Shared.Tests.CQRS;

public sealed record AbstractCommand : ICommand<string>;
