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
    [InlineData("fr")]
    public void Constructor_ValidValue_CreatesInstance(string value)
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

    [Fact]
    public void Constructor_AtMaxLength_CreatesInstance()
    {
        var language = PreferredLanguage.From(new string('a', 10));

        language.Value.Should().HaveLength(10);
    }

    [Fact]
    public void Constructor_ExceedsMaxLength_ThrowsGuardException()
    {
        var act = () => PreferredLanguage.From(new string('a', 11));

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var lang1 = PreferredLanguage.From("en");
        var lang2 = PreferredLanguage.From("en");

        lang1.Should().Be(lang2);
    }
}
