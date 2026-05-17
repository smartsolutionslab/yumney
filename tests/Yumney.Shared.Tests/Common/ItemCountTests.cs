using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.Shared.Paging;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Common;

public class ItemCountTests
{
	[Fact]
	public void From_Zero_IsAccepted()
	{
		var count = ItemCount.From(0);

		count.Value.Should().Be(0);
	}

	[Fact]
	public void From_PositiveValue_StoresValue()
	{
		var count = ItemCount.From(42);

		count.Value.Should().Be(42);
	}

	[Fact]
	public void From_NegativeValue_Throws()
	{
		var act = () => ItemCount.From(-1);

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void ImplicitConversionToInt_YieldsValue()
	{
		var count = ItemCount.From(7);

		int raw = count;

		raw.Should().Be(7);
	}

	[Fact]
	public void ToString_UsesInvariantCulture()
	{
		var count = ItemCount.From(1000);

		count.ToString().Should().Be("1000");
	}

	[Fact]
	public void Equality_SameValue_AreEqual()
	{
		var first = ItemCount.From(5);
		var second = ItemCount.From(5);

		first.Should().Be(second);
	}
}
