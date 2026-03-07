using FluentAssertions;
using Xunit;
using Yumney.Shared.Guards;
using Yumney.Users.Domain.AppUserProfile;

namespace Yumney.Users.Domain.Tests.AppUserProfile;

public class PasswordTests
{
    [Theory]
    [InlineData("Password1")]
    [InlineData("Abcdef1x")]
    [InlineData("C0mpl3xP@ss")]
    public void Constructor_ValidPassword_CreatesInstance(string value)
    {
        var password = new Password(value);

        password.Value.Should().Be(value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrWhitespace_ThrowsGuardException(string? value)
    {
        var act = () => new Password(value!);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Constructor_TooShort_ThrowsGuardException()
    {
        var act = () => new Password("Short1A");

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Constructor_NoUppercase_ThrowsGuardException()
    {
        var act = () => new Password("nouppercase1");

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Constructor_NoLowercase_ThrowsGuardException()
    {
        var act = () => new Password("NOLOWERCASE1");

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Constructor_NoDigit_ThrowsGuardException()
    {
        var act = () => new Password("NoDigitsHere");

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void ToString_ReturnsMaskedValue()
    {
        var password = new Password("Password1");

        password.ToString().Should().Be("***");
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var password1 = new Password("Password1");
        var password2 = new Password("Password1");

        password1.Should().Be(password2);
    }
}
