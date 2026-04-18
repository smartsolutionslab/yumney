using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

public sealed record IngredientIdentifier : IValueObject
{
	public Guid Value { get; }

	private IngredientIdentifier(Guid value)
	{
		Value = Ensure.That(value).IsNotEmpty().AndReturn();
	}

	public static IngredientIdentifier New() => new(Guid.CreateVersion7());

	public static IngredientIdentifier From(Guid value) => new(value);

	public override string ToString() => Value.ToString();
}
