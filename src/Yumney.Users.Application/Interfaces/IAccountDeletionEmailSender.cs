using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

namespace SmartSolutionsLab.Yumney.Users.Application.Interfaces;

/// <summary>
/// Sends the GDPR account-deletion confirmation email. The message is
/// localised by the user's <see cref="PreferredLanguage" /> captured before
/// purge — by send-time the profile row is already gone.
/// </summary>
public interface IAccountDeletionEmailSender
{
	Task SendAsync(AccountDeletionEmailPayload payload, CancellationToken cancellationToken = default);
}

/// <summary>
/// Snapshot of the data needed to render the deletion-confirmation email,
/// captured from the user's profile + Keycloak account before the GDPR purge
/// runs.
/// </summary>
public sealed record AccountDeletionEmailPayload(
	Email RecipientEmail,
	DisplayName DisplayName,
	PreferredLanguage Language);
