using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Quantities;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Common;

public class RoundedQuantityTests
{
	[Fact]
	public void WasRounded_DifferentValues_IsTrue()
	{
		var quantity = new RoundedQuantity(DisplayQuantity: 2m, ExactQuantity: 1.78m);

		quantity.WasRounded.Should().BeTrue();
	}

	[Fact]
	public void WasRounded_EqualValues_IsFalse()
	{
		var quantity = new RoundedQuantity(DisplayQuantity: 3m, ExactQuantity: 3m);

		quantity.WasRounded.Should().BeFalse();
	}

	[Fact]
	public void Ctor_StoresBothQuantities()
	{
		var quantity = new RoundedQuantity(DisplayQuantity: 1.5m, ExactQuantity: 1.42m);

		quantity.DisplayQuantity.Should().Be(1.5m);
		quantity.ExactQuantity.Should().Be(1.42m);
	}
}
