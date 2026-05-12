using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using SmartSolutionsLab.Yumney.Users.Application.Interfaces;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Services;

#pragma warning disable SA1601
public sealed partial class SmtpAccountDeletionEmailSender(
	IOptions<SmtpOptions> options,
	ILogger<SmtpAccountDeletionEmailSender> logger)
	: IAccountDeletionEmailSender
{
	public async Task SendAsync(AccountDeletionEmailPayload payload, CancellationToken cancellationToken = default)
	{
		var settings = options.Value;
		var template = AccountDeletionEmailTemplate.Render(payload);

		var message = new MimeMessage();
		message.From.Add(new MailboxAddress(settings.FromDisplayName, settings.FromAddress));
		message.To.Add(new MailboxAddress(payload.DisplayName.Value, payload.RecipientEmail.Value));
		message.Subject = template.Subject;
		message.Body = new TextPart("plain") { Text = template.Body };

		using var client = new SmtpClient();
		var socketOptions = settings.UseStartTls
			? SecureSocketOptions.StartTls
			: SecureSocketOptions.None;
		await client.ConnectAsync(settings.Host, settings.Port, socketOptions, cancellationToken);
		if (!string.IsNullOrEmpty(settings.Username))
		{
			await client.AuthenticateAsync(settings.Username, settings.Password ?? string.Empty, cancellationToken);
		}

		await client.SendAsync(message, cancellationToken);
		await client.DisconnectAsync(quit: true, cancellationToken);

		LogConfirmationSent(payload.RecipientEmail.Value);
	}

	[LoggerMessage(Level = LogLevel.Information, Message = "GDPR: account-deletion confirmation email sent to {EmailAddress}")]
	private partial void LogConfirmationSent(string emailAddress);
}
