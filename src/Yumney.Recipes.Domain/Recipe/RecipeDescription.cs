using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

public sealed record RecipeDescription : IValueObject<string>
{
	public const int MaxLength = 2000;

	public string Value { get; }

	private RecipeDescription(string value)
	{
		string validated = Ensure.That(value).IsNotNullOrWhiteSpace().HasMaxLength(MaxLength).AndReturn();
		Value = validated.Trim();
	}

	public static RecipeDescription From(string value) => new(value);

	public static RecipeDescription? FromNullable(string? value) => value.HasValue() ? new RecipeDescription(value!) : null;

	public static implicit operator string(RecipeDescription description) => description.Value;
}
