using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Builders;

public sealed class RecipeLanguageBuilder
{
	private string value = "en";

	public static RecipeLanguageBuilder A() => new();

	public RecipeLanguageBuilder With(string value)
	{
		this.value = value;
		return this;
	}

	public RecipeLanguage Build() => RecipeLanguage.From(value);

	public static implicit operator RecipeLanguage(RecipeLanguageBuilder builder) => builder.Build();
}
