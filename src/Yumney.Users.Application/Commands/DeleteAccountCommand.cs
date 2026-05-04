using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Users.Application.Commands;

/// <summary>
/// Permanently erases the current user's account (US-101 / GDPR Art. 17).
/// The handler resolves the user from <see cref="ICurrentUser"/>; the command
/// itself is parameterless to prevent accidental cross-user deletion from a
/// crafted request.
/// </summary>
public sealed record DeleteAccountCommand : ICommand<Result>;
