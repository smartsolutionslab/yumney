using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Quantities;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Common;

public class FreshnessExtensionsTests
{
	[Theory]
	[InlineData(Freshness.NotTracked, 0)]
	[InlineData(Freshness.Fresh, 1)]
	[InlineData(Freshness.UseSoon, 2)]
	[InlineData(Freshness.CheckIt, 3)]
	public void Urgency_ReturnsExpectedRank(Freshness freshness, int expected)
	{
		freshness.Urgency().Should().Be(expected);
	}

	[Theory]
	[InlineData(Freshness.NotTracked, false)]
	[InlineData(Freshness.Fresh, false)]
	[InlineData(Freshness.UseSoon, true)]
	[InlineData(Freshness.CheckIt, true)]
	public void IsUrgent_TrueForUseSoonAndCheckIt(Freshness freshness, bool expected)
	{
		freshness.IsUrgent().Should().Be(expected);
	}
}
