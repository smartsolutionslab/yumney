using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Guards;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Common;

public class AggregateVersionTests
{
	[Fact]
	public void Zero_ReturnsVersionWithValueZero()
	{
		AggregateVersion.Zero().Value.Should().Be(0);
	}

	[Fact]
	public void From_PositiveValue_StoresValue()
	{
		AggregateVersion.From(7).Value.Should().Be(7);
	}

	[Fact]
	public void From_NegativeValue_Throws()
	{
		var act = () => AggregateVersion.From(-1);

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void Increment_ReturnsNewVersion_LeavesOriginalUnchanged()
	{
		var original = AggregateVersion.From(3);

		var next = original.Increment();

		next.Value.Should().Be(4);
		original.Value.Should().Be(3);
	}

	[Fact]
	public void ImplicitConversionToInt_YieldsValue()
	{
		var version = AggregateVersion.From(12);

		int raw = version;

		raw.Should().Be(12);
	}

	[Fact]
	public void ToString_UsesInvariantCulture()
	{
		AggregateVersion.From(1234).ToString().Should().Be("1234");
	}

	[Fact]
	public void Equality_SameValue_AreEqual()
	{
		AggregateVersion.From(5).Should().Be(AggregateVersion.From(5));
	}
}
