using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Commands;

public sealed record EndShoppingModeCommand(bool AcceptPendingChanges) : ICommand<Result>;
