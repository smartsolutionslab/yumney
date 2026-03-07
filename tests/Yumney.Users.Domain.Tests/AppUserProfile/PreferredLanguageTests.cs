using FluentAssertions;
using Xunit;
using Yumney.Shared.Guards;
using Yumney.Users.Domain.AppUserProfile;

namespace Yumney.Users.Domain.Tests.AppUserProfile;

public class PreferredLanguageTests
{
    [Theory]
    [InlineData("en")]
    [InlineData("de")]
    [InlineData("fr")]
    public void Constructor_ValidValue_CreatesInstance(string value)
    {
        var language = new PreferredLanguage(value);

        language.Value.Should().Be(value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrWhitespace_ThrowsGuardException(string? value)
    {
        var act = () => new PreferredLanguage(value!);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Constructor_AtMaxLength_CreatesInstance()
    {
        var language = new PreferredLanguage(new string('a', 10));

        language.Value.Should().HaveLength(10);
    }

    [Fact]
    public void Constructor_ExceedsMaxLength_ThrowsGuardException()
    {
        var act = () => new PreferredLanguage(new string('a', 11));

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var language = new PreferredLanguage("en");

        language.ToString().Should().Be("en");
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var lang1 = new PreferredLanguage("en");
        var lang2 = new PreferredLanguage("en");

        lang1.Should().Be(lang2);
    }
}
