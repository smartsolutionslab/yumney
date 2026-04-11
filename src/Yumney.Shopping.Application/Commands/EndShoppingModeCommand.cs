using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Commands;

public sealed record EndShoppingModeCommand(bool AcceptPendingChanges) : ICommand<Result>;
