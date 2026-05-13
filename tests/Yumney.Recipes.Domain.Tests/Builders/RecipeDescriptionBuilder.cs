using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Builders;

public sealed class RecipeDescriptionBuilder
{
	private string value = "Test description";

	public static RecipeDescriptionBuilder A() => new();

	public RecipeDescriptionBuilder With(string value)
	{
		this.value = value;
		return this;
	}

	public RecipeDescription Build() => RecipeDescription.From(value);

	public static implicit operator RecipeDescription(RecipeDescriptionBuilder builder) => builder.Build();
}
