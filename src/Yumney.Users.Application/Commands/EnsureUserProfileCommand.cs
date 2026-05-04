using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.Users.Application.Commands;

/// <summary>
/// Ensures the current authenticated user has an <c>AppUserProfile</c> row,
/// creating one from JWT claims if missing. Idempotent — safe to call on
/// every authenticated profile request.
/// </summary>
public sealed record EnsureUserProfileCommand : ICommand<Result>;
