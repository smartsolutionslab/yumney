using System;
using System.Collections.Generic;
using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Shared.Quantities;

#pragma warning disable SA1311
public sealed record IngredientCategory : IValueObject<string>
{
	public const int MaxLength = 30;

#pragma warning disable SA1202
	private static readonly string[] allowedValues =
		["produce", "dairy", "meat-fish", "bakery", "frozen", "beverages", "pantry", "spices", "household", "other"];

	public static readonly IngredientCategory Produce = new("produce");
	public static readonly IngredientCategory Dairy = new("dairy");
	public static readonly IngredientCategory MeatFish = new("meat-fish");
	public static readonly IngredientCategory Bakery = new("bakery");
	public static readonly IngredientCategory Frozen = new("frozen");
	public static readonly IngredientCategory Beverages = new("beverages");
	public static readonly IngredientCategory Pantry = new("pantry");
	public static readonly IngredientCategory Spices = new("spices");
	public static readonly IngredientCategory Household = new("household");
	public static readonly IngredientCategory Other = new("other");
#pragma warning restore SA1202

	public string Value { get; }

	public int DisplayOrder { get; }

	private IngredientCategory(string value)
	{
		Value = Ensure.That(value)
			.IsNotNullOrWhiteSpace()
			.HasMaxLength(MaxLength)
			.IsOneOf(allowedValues)
			.AndReturn();
		DisplayOrder = Array.IndexOf(allowedValues, Value);
	}

	public static IngredientCategory From(string value) => new(value);

	public static IReadOnlyList<IngredientCategory> All { get; } =
		[Produce, Dairy, MeatFish, Bakery, Frozen, Beverages, Pantry, Spices, Household, Other];

	public static implicit operator string(IngredientCategory obj) => obj.Value;
}
#pragma warning restore SA1311
