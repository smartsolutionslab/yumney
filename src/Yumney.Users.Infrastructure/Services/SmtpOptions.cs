namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Services;

/// <summary>
/// SMTP transport settings for outbound email. Bound from the
/// <c>Smtp</c> configuration section by <c>AddUsersInfrastructure</c>.
/// Local development uses Mailpit (port 1025, no auth, no TLS); the
/// AppHost wires this in run mode and a Container App secret in publish.
/// </summary>
public sealed class SmtpOptions
{
	public const string SectionName = "Smtp";

	public string Host { get; init; } = string.Empty;

	public int Port { get; init; } = 1025;

	public string? Username { get; init; }

	public string? Password { get; init; }

	public bool UseStartTls { get; init; }

	public string FromAddress { get; init; } = "noreply@yumney.local";

	public string FromDisplayName { get; init; } = "Yumney";
}
