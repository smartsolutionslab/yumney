using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Domain.Tests.AppUserProfile;

public class PasswordTests
{
	[Theory]
	[InlineData("Password1")]
	[InlineData("Abcdef1x")]
	[InlineData("C0mpl3xP@ss")]
	public void Constructor_ValidPassword_CreatesInstance(string value)
	{
		var password = Password.From(value);

		password.Value.Should().Be(value);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void Constructor_NullOrWhitespace_ThrowsGuardException(string? value)
	{
		var act = () => Password.From(value!);

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void Constructor_TooShort_ThrowsGuardException()
	{
		var act = () => Password.From("Short1A");

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void Constructor_NoUppercase_ThrowsGuardException()
	{
		var act = () => Password.From("nouppercase1");

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void Constructor_NoLowercase_ThrowsGuardException()
	{
		var act = () => Password.From("NOLOWERCASE1");

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void Constructor_NoDigit_ThrowsGuardException()
	{
		var act = () => Password.From("NoDigitsHere");

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void ToString_ReturnsMaskedValue()
	{
		var password = Password.From("Password1");

		password.ToString().Should().Be("***");
	}

	[Fact]
	public void Equality_SameValue_AreEqual()
	{
		var password1 = Password.From("Password1");
		var password2 = Password.From("Password1");

		password1.Should().Be(password2);
	}
}
