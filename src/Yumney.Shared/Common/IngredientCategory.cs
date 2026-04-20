using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Shared.Common;

#pragma warning disable SA1311
public sealed record IngredientCategory : IValueObject<string>
{
	public const int MaxLength = 30;

#pragma warning disable SA1202
	private static readonly string[] allowedValues =
	[
		Produce!.Value,
		Dairy!.Value,
		MeatFish!.Value,
		Bakery!.Value,
		Frozen!.Value,
		Beverages!.Value,
		Pantry!.Value,
		Household!.Value,
		Other!.Value
	];

	public static readonly IngredientCategory Produce = From("produce");
	public static readonly IngredientCategory Dairy = From("dairy");
	public static readonly IngredientCategory MeatFish = From("meat-fish");
	public static readonly IngredientCategory Bakery = From("bakery");
	public static readonly IngredientCategory Frozen = From("frozen");
	public static readonly IngredientCategory Beverages = From("beverages");
	public static readonly IngredientCategory Pantry = From("pantry");
	public static readonly IngredientCategory Household = From("household");
	public static readonly IngredientCategory Other = From("other");
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
		[Produce, Dairy, MeatFish, Bakery, Frozen, Beverages, Pantry, Household, Other];

	public static implicit operator string(IngredientCategory obj) => obj.Value;
}
#pragma warning restore SA1311
