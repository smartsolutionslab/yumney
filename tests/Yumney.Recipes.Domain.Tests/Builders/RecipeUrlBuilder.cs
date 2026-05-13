using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Builders;

public sealed class RecipeUrlBuilder
{
	private string value = "https://example.com/recipe";

	public static RecipeUrlBuilder A() => new();

	public RecipeUrlBuilder With(string value)
	{
		this.value = value;
		return this;
	}

	public RecipeUrl Build() => RecipeUrl.From(value);

	public static implicit operator RecipeUrl(RecipeUrlBuilder builder) => builder.Build();
}
