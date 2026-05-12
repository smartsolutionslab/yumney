using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

public sealed record RecipeIdentifier : IValueObject
{
	public Guid Value { get; }

	private RecipeIdentifier(Guid value)
	{
		Value = Ensure.That(value).IsNotEmpty().AndReturn();
	}

	public static RecipeIdentifier New() => new(Guid.CreateVersion7());

	public static RecipeIdentifier From(Guid value) => new(value);

	public static RecipeIdentifier? FromNullable(Guid? value) =>
		value.HasValue ? new RecipeIdentifier(value.Value) : null;

	public static RecipeIdentifier? FromNullable(string value)
	{
		if (!Guid.TryParse(value, out var guid)) return null;

		return FromNullable(guid);
	}

	public override string ToString() => Value.ToString();
}
