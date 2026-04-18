using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

public sealed record IngredientName : IValueObject<string>
{
	public const int MaxLength = 200;

	public string Value { get; }

	private IngredientName(string value)
	{
		string validated = Ensure.That(value)
			.IsNotNullOrWhiteSpace()
			.HasMaxLength(MaxLength)
			.AndReturn();
		Value = validated.Trim();
	}

	public static IngredientName From(string value) => new(value);

	public static implicit operator string(IngredientName name) => name.Value;
}
