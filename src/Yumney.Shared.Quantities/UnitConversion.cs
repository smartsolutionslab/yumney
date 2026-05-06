using System;
using System.Collections.Generic;

namespace SmartSolutionsLab.Yumney.Shared.Quantities;

/// <summary>
/// Pure utility for converting cooking ingredient amounts between metric
/// and imperial. Rounding is intentionally coarse — kitchens don't measure
/// <c>1.7834 oz</c>. See <see cref="SmartRound(decimal)"/>.
///
/// Unknown / count units (pinch, slice, clove, "to taste", …) are returned
/// unchanged so callers can pipe every ingredient through this service
/// without filtering first.
/// </summary>
public static class UnitConversion
{
#pragma warning disable SA1311 // editorconfig requires camelCase for private fields
	private static readonly IReadOnlyDictionary<string, ConversionRule> metricToImperialRules = new Dictionary<string, ConversionRule>(StringComparer.OrdinalIgnoreCase)
	{
		["g"] = new("oz", 1m / 28.3495m),
		["kg"] = new("lb", 2.20462m),
		["ml"] = new("fl oz", 1m / 29.5735m),
		["cl"] = new("fl oz", 10m / 29.5735m),
		["dl"] = new("cup", 100m / 236.588m),
		["l"] = new("cup", 1000m / 236.588m),
	};

	private static readonly IReadOnlyDictionary<string, ConversionRule> imperialToMetricRules = new Dictionary<string, ConversionRule>(StringComparer.OrdinalIgnoreCase)
	{
		["oz"] = new("g", 28.3495m),
		["lb"] = new("g", 453.592m),
		["fl oz"] = new("ml", 29.5735m),
		["cup"] = new("ml", 236.588m),
		["tsp"] = new("ml", 4.92892m),
		["tbsp"] = new("ml", 14.7868m),
	};
#pragma warning restore SA1311

	public static ConvertedAmount ToImperial(decimal amount, string? unit) =>
		Apply(amount, unit, metricToImperialRules);

	public static ConvertedAmount ToMetric(decimal amount, string? unit) =>
		Apply(amount, unit, imperialToMetricRules);

	public static ConvertedAmount ToSystem(decimal amount, string? unit, UnitSystem target) =>
		target == UnitSystem.Imperial ? ToImperial(amount, unit) : ToMetric(amount, unit);

	/// <summary>Celsius → Fahrenheit. Symmetric helper for oven-temperature conversion.</summary>
	/// <param name="celsius">Temperature in Celsius.</param>
	/// <returns>The same temperature in Fahrenheit, rounded to the nearest whole degree.</returns>
	public static int CelsiusToFahrenheit(int celsius) =>
		(int)Math.Round((celsius * 9d / 5d) + 32d);

	/// <summary>Fahrenheit → Celsius.</summary>
	/// <param name="fahrenheit">Temperature in Fahrenheit.</param>
	/// <returns>The same temperature in Celsius, rounded to the nearest whole degree.</returns>
	public static int FahrenheitToCelsius(int fahrenheit) =>
		(int)Math.Round((fahrenheit - 32) * 5d / 9d);

	/// <summary>
	/// Coerce a converted amount to a number a cook can actually measure.
	/// The thresholds mirror what real cookbooks do — no <c>0.34 oz</c>,
	/// no <c>127.4 g</c>. Exposed for spec coverage.
	/// </summary>
	/// <param name="value">Raw decimal amount, typically the product of a unit conversion.</param>
	/// <returns>The cooking-friendly rounded amount.</returns>
	public static decimal SmartRound(decimal value)
	{
		var absolute = Math.Abs(value);
		if (absolute < 1m) return RoundToStep(value, 0.25m);
		if (absolute < 10m) return RoundToStep(value, 0.5m);
		if (absolute < 100m) return Math.Round(value, MidpointRounding.AwayFromZero);
		if (absolute < 1000m) return RoundToStep(value, 5m);
		return RoundToStep(value, 10m);
	}

	private static ConvertedAmount Apply(decimal amount, string? unit, IReadOnlyDictionary<string, ConversionRule> table)
	{
		var normalized = (unit ?? string.Empty).Trim();
		if (normalized.Length == 0) return new ConvertedAmount(SmartRound(amount), string.Empty);

		if (!table.TryGetValue(normalized, out var rule)) return new ConvertedAmount(amount, normalized);

		var converted = amount * rule.Factor;
		return new ConvertedAmount(SmartRound(converted), rule.TargetUnit);
	}

	private static decimal RoundToStep(decimal value, decimal step) =>
		Math.Round(value / step, MidpointRounding.AwayFromZero) * step;

	private sealed record ConversionRule(string TargetUnit, decimal Factor);
}
