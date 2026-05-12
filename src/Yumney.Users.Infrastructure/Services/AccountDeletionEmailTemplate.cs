using SmartSolutionsLab.Yumney.Users.Application.Interfaces;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Services;

/// <summary>
/// Renders the GDPR account-deletion confirmation. Two locales (EN / DE)
/// inlined as constants rather than embedded resources — the message is
/// fixed and short enough that a templating engine would be overkill.
/// Add cases when new <see cref="Yumney.Users.Domain.AppUserProfile.PreferredLanguage"/>
/// values appear.
/// </summary>
internal static class AccountDeletionEmailTemplate
{
	public static AccountDeletionEmail Render(AccountDeletionEmailPayload payload)
	{
		var name = payload.DisplayName.Value;
		return payload.Language.Value == "de"
			? new AccountDeletionEmail(GermanSubject, RenderGermanBody(name))
			: new AccountDeletionEmail(EnglishSubject, RenderEnglishBody(name));
	}

#pragma warning disable SA1303
	private const string englishSubject = "Your Yumney account has been deleted";
	private const string germanSubject = "Dein Yumney-Konto wurde gelöscht";
#pragma warning restore SA1303

	public static string EnglishSubject => englishSubject;

	public static string GermanSubject => germanSubject;

	private static string RenderEnglishBody(string name) => $"""
        Hi {name},

        your account-deletion request has been processed. We've permanently erased the following data:

        • Profile (display name, language, preferences)
        • Activity history
        • Staples and shopping lists
        • Meal plans and cooked-meal records
        • Imported recipes
        • Your Keycloak sign-in

        If you didn't request this deletion, please contact support@yumney.local right away.

        We hope to see you again sometime.
        The Yumney team
        """;

	private static string RenderGermanBody(string name) => $"""
        Hallo {name},

        deine Anfrage zur Kontolöschung wurde bearbeitet. Wir haben die folgenden Daten unwiderruflich entfernt:

        • Profil (Anzeigename, Sprache, Einstellungen)
        • Aktivitäts-Verlauf
        • Vorratslisten und Einkaufslisten
        • Wochenplanung und gekochte Mahlzeiten
        • Importierte Rezepte
        • Dein Login bei Keycloak

        Falls du diese Löschung nicht angefordert hast, kontaktiere bitte umgehend support@yumney.local.

        Wir hoffen, dich irgendwann wiederzusehen.
        Dein Yumney-Team
        """;
}

internal sealed record AccountDeletionEmail(string Subject, string Body);
