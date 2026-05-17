using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.Users.Domain.StaplesList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Domain.Tests.StaplesList;

public class OwnerIdentifierTests
{
	[Fact]
	public void From_TrimsSurroundingWhitespace()
	{
		var owner = OwnerIdentifier.From("  kc-user-1  ");

		owner.Value.Should().Be("kc-user-1");
	}

	[Fact]
	public void From_AtMaxLength_IsAccepted()
	{
		var owner = OwnerIdentifier.From(new string('x', OwnerIdentifier.MaxLength));

		owner.Value.Length.Should().Be(OwnerIdentifier.MaxLength);
	}

	[Fact]
	public void From_BeyondMaxLength_Throws()
	{
		var act = () => OwnerIdentifier.From(new string('x', OwnerIdentifier.MaxLength + 1));

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void From_Empty_Throws()
	{
		var act = () => OwnerIdentifier.From(string.Empty);

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void From_Whitespace_Throws()
	{
		var act = () => OwnerIdentifier.From("   ");

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void ImplicitConversion_ToString_YieldsValue()
	{
		var owner = OwnerIdentifier.From("kc-user-1");
		string raw = owner;

		raw.Should().Be("kc-user-1");
	}

	[Fact]
	public void Equality_SameValue_AreEqual()
	{
		var a = OwnerIdentifier.From("kc-user-1");
		var b = OwnerIdentifier.From("kc-user-1");

		a.Should().Be(b);
		a.GetHashCode().Should().Be(b.GetHashCode());
	}
}
