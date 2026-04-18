using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Domain.Tests.AppUserProfile;

public class PreferredLanguageTests
{
	[Theory]
	[InlineData("en")]
	[InlineData("de")]
	public void Constructor_SupportedValue_CreatesInstance(string value)
	{
		var language = PreferredLanguage.From(value);

		language.Value.Should().Be(value);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void Constructor_NullOrWhitespace_ThrowsGuardException(string? value)
	{
		var act = () => PreferredLanguage.From(value!);

		act.Should().Throw<GuardException>();
	}

	[Theory]
	[InlineData("fr")]
	[InlineData("EN")]
	[InlineData("english")]
	[InlineData("xx")]
	public void Constructor_UnsupportedLanguage_ThrowsGuardException(string value)
	{
		var act = () => PreferredLanguage.From(value);

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void English_StaticInstance_HasValueEn()
	{
		PreferredLanguage.English.Value.Should().Be("en");
	}

	[Fact]
	public void German_StaticInstance_HasValueDe()
	{
		PreferredLanguage.German.Value.Should().Be("de");
	}

	[Fact]
	public void Equality_SameValue_AreEqual()
	{
		var lang1 = PreferredLanguage.From("en");
		var lang2 = PreferredLanguage.From("en");

		lang1.Should().Be(lang2);
	}
}
