using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Quantities;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Common;

public class UnitConversionTests
{
	[Fact]
	public void ToImperial_500g_ReturnsRoughly18oz()
	{
		// TC-125-01: 500g → ~17.6 oz, smart-rounded.
		var result = UnitConversion.ToImperial(500m, "g");

		result.Unit.Should().Be("oz");
		result.Amount.Should().Be(18m);
	}

	[Fact]
	public void ToImperial_200ml_ReturnsRoughly7FlOz()
	{
		// TC-125-02: 200ml → ~6.76 fl oz.
		var result = UnitConversion.ToImperial(200m, "ml");

		result.Unit.Should().Be("fl oz");
		result.Amount.Should().BeApproximately(7m, 0.5m);
	}

	[Fact]
	public void ToImperial_1Kilogram_ReturnsRoughly2Pounds()
	{
		var result = UnitConversion.ToImperial(1m, "kg");

		result.Unit.Should().Be("lb");
		result.Amount.Should().BeApproximately(2m, 0.5m);
	}

	[Fact]
	public void ToImperial_1Litre_ReturnsRoughly4Cups()
	{
		var result = UnitConversion.ToImperial(1m, "l");

		result.Unit.Should().Be("cup");
		result.Amount.Should().Be(4m);
	}

	[Fact]
	public void ToImperial_UpperCaseUnit_StillMatches()
	{
		var lower = UnitConversion.ToImperial(500m, "g");
		var upper = UnitConversion.ToImperial(500m, "G");

		upper.Should().Be(lower);
	}

	[Theory]
	[InlineData("pinch")]
	[InlineData("slice")]
	[InlineData("clove")]
	[InlineData("dash")]
	public void ToImperial_NonConvertibleCountUnit_ReturnsAmountUnchanged(string unit)
	{
		// TC-125-05: pass-through for non-convertible count units.
		var result = UnitConversion.ToImperial(2m, unit);

		result.Should().Be(new ConvertedAmount(2m, unit));
	}

	[Fact]
	public void ToImperial_NullOrEmptyUnit_ReturnsEmptyUnit()
	{
		UnitConversion.ToImperial(3m, null).Should().Be(new ConvertedAmount(3m, string.Empty));
		UnitConversion.ToImperial(3m, string.Empty).Should().Be(new ConvertedAmount(3m, string.Empty));
		UnitConversion.ToImperial(3m, "   ").Should().Be(new ConvertedAmount(3m, string.Empty));
	}

	[Fact]
	public void ToMetric_1Cup_ReturnsRoughly235ml()
	{
		var result = UnitConversion.ToMetric(1m, "cup");

		result.Unit.Should().Be("ml");
		result.Amount.Should().Be(235m);
	}

	[Fact]
	public void ToMetric_1Pound_ReturnsRoughly455g()
	{
		var result = UnitConversion.ToMetric(1m, "lb");

		result.Unit.Should().Be("g");
		result.Amount.Should().Be(455m);
	}

	[Fact]
	public void ToMetric_1Tablespoon_Returns15ml()
	{
		var result = UnitConversion.ToMetric(1m, "tbsp");

		result.Unit.Should().Be("ml");
		result.Amount.Should().Be(15m);
	}

	[Theory]
	[InlineData(UnitSystem.Imperial, "g", "oz")]
	[InlineData(UnitSystem.Metric, "cup", "ml")]
	public void ToSystem_RoutesToTheCorrectTable(UnitSystem target, string fromUnit, string expectedUnit)
	{
		var result = UnitConversion.ToSystem(100m, fromUnit, target);

		result.Unit.Should().Be(expectedUnit);
	}

	[Fact]
	public void CelsiusToFahrenheit_180C_Is356F()
	{
		// TC-125-03.
		UnitConversion.CelsiusToFahrenheit(180).Should().Be(356);
	}

	[Fact]
	public void Temperature_RoundTripsWithinOneDegree()
	{
		var celsius = UnitConversion.FahrenheitToCelsius(UnitConversion.CelsiusToFahrenheit(180));

		celsius.Should().BeInRange(179, 181);
	}

	[Theory]
	[InlineData(0.34, 0.25)]
	[InlineData(0.13, 0.25)]
	[InlineData(0.05, 0)]
	[InlineData(1.2, 1)]
	[InlineData(2.4, 2.5)]
	[InlineData(7.8, 8)]
	[InlineData(17.6, 18)]
	[InlineData(99.5, 100)]
	[InlineData(127.4, 125)]
	[InlineData(503.2, 505)]
	[InlineData(1234.7, 1230)]
	public void SmartRound_RoundsToCookingFriendlyValues(decimal input, decimal expected)
	{
		UnitConversion.SmartRound(input).Should().Be(expected);
	}
}
