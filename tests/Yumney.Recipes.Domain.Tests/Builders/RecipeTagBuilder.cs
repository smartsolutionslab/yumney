using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Builders;

public sealed class RecipeTagBuilder
{
	private string value = "vegetarian";

	public static RecipeTagBuilder A() => new();

	public RecipeTagBuilder With(string value)
	{
		this.value = value;
		return this;
	}

	public RecipeTag Build() => RecipeTag.From(value);

	public static implicit operator RecipeTag(RecipeTagBuilder builder) => builder.Build();
}
