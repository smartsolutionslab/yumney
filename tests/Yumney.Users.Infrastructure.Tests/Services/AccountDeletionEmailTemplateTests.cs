using FluentAssertions;
using SmartSolutionsLab.Yumney.Users.Application.Interfaces;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;
using SmartSolutionsLab.Yumney.Users.Infrastructure.Services;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Tests.Services;

public class AccountDeletionEmailTemplateTests
{
	[Fact]
	public void Render_GermanLocale_UsesGermanSubject()
	{
		var payload = MakePayload("de");

		var email = AccountDeletionEmailTemplate.Render(payload);

		email.Subject.Should().Be(AccountDeletionEmailTemplate.GermanSubject);
		email.Body.Should().Contain("Hallo Anna").And.Contain("Vorratslisten");
	}

	[Fact]
	public void Render_EnglishLocale_UsesEnglishSubject()
	{
		var payload = MakePayload("en");

		var email = AccountDeletionEmailTemplate.Render(payload);

		email.Subject.Should().Be(AccountDeletionEmailTemplate.EnglishSubject);
		email.Body.Should().Contain("Hi Anna").And.Contain("Staples and shopping lists");
	}

	[Fact]
	public void Render_BothLocales_MentionTheEraseScope()
	{
		// AC: the email confirms the data categories that were erased.
		// Spot-check that the German and English bodies both reference the
		// canonical categories — a missing line would mean we silently
		// dropped a category from one locale during a future edit.
		var de = AccountDeletionEmailTemplate.Render(MakePayload("de"));
		var en = AccountDeletionEmailTemplate.Render(MakePayload("en"));

		foreach (var fragment in new[] { "Profil", "Aktivitäts", "Wochenplanung", "Keycloak" })
		{
			de.Body.Should().Contain(fragment);
		}

		foreach (var fragment in new[] { "Profile", "Activity", "Meal plans", "Keycloak" })
		{
			en.Body.Should().Contain(fragment);
		}
	}

	private static AccountDeletionEmailPayload MakePayload(string language) => new(
		Email.From("anna@example.com"),
		DisplayName.From("Anna"),
		PreferredLanguage.From(language));
}
