using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.Tests.ShoppingLedger;

public class ShoppingLedgerIdentifierTests
{
	[Fact]
	public void From_ValidGuid_CreatesInstance()
	{
		var guid = Guid.NewGuid();

		var identifier = ShoppingLedgerIdentifier.From(guid);

		identifier.Value.Should().Be(guid);
	}

	[Fact]
	public void From_EmptyGuid_ThrowsGuardException()
	{
		var act = () => ShoppingLedgerIdentifier.From(Guid.Empty);

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void CreateNew_ReturnsNonEmptyIdentifier()
	{
		var identifier = ShoppingLedgerIdentifier.CreateNew();

		identifier.Value.Should().NotBeEmpty();
	}

	[Fact]
	public void CreateNew_ReturnsUniqueValues()
	{
		var a = ShoppingLedgerIdentifier.CreateNew();
		var b = ShoppingLedgerIdentifier.CreateNew();

		a.Should().NotBe(b);
	}

	[Fact]
	public void ImplicitConversion_ReturnsGuid()
	{
		var guid = Guid.NewGuid();
		Guid result = ShoppingLedgerIdentifier.From(guid);

		result.Should().Be(guid);
	}
}
