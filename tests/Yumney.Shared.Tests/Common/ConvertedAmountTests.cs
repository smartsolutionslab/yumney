using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Quantities;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Common;

public class ConvertedAmountTests
{
	[Fact]
	public void Ctor_StampsAmountAndUnit()
	{
		var converted = new ConvertedAmount(8m, "oz");

		converted.Amount.Should().Be(8m);
		converted.Unit.Should().Be("oz");
	}

	[Fact]
	public void Equality_SameValues_AreEqual()
	{
		var first = new ConvertedAmount(250m, "ml");
		var second = new ConvertedAmount(250m, "ml");

		first.Should().Be(second);
	}
}
