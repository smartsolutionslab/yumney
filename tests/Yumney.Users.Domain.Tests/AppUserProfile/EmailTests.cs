using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Domain.Tests.AppUserProfile;

public class EmailTests
{
	[Theory]
	[InlineData("test@example.com")]
	[InlineData("user+tag@example.com")]
	[InlineData("user.name@example.co.uk")]
	public void Constructor_ValidEmail_CreatesInstance(string value)
	{
		var email = Email.From(value);

		email.Value.Should().Be(value.Trim().ToLowerInvariant());
	}

	[Fact]
	public void Constructor_TrimsAndLowerCases()
	{
		var email = Email.From("  Test@Example.COM  ");

		email.Value.Should().Be("test@example.com");
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void Constructor_NullOrWhitespace_ThrowsGuardException(string? value)
	{
		var act = () => Email.From(value!);

		act.Should().Throw<GuardException>();
	}

	[Theory]
	[InlineData("not-an-email")]
	[InlineData("@missing-local.com")]
	[InlineData("missing-domain@")]
	public void Constructor_InvalidFormat_ThrowsGuardException(string value)
	{
		var act = () => Email.From(value);

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void Constructor_AtMaxLength_CreatesInstance()
	{
		var localPart = new string('a', 242);
		var email = $"{localPart}@example.com";

		var result = Email.From(email);

		result.Value.Should().HaveLength(254);
	}

	[Fact]
	public void Constructor_ExceedsMaxLength_ThrowsGuardException()
	{
		var localPart = new string('a', 243);
		var email = $"{localPart}@example.com";

		var act = () => Email.From(email);

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void Constructor_DifferentCasing_NormalizesToSameValue()
	{
		var email1 = Email.From("User@Example.COM");
		var email2 = Email.From("user@example.com");

		email1.Should().Be(email2);
	}

	[Fact]
	public void Equality_SameValue_AreEqual()
	{
		var email1 = Email.From("test@example.com");
		var email2 = Email.From("test@example.com");

		email1.Should().Be(email2);
	}

	[Fact]
	public void Equality_DifferentValue_AreNotEqual()
	{
		var email1 = Email.From("one@example.com");
		var email2 = Email.From("two@example.com");

		email1.Should().NotBe(email2);
	}
}
