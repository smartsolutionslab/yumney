using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Builders;

/// <summary>
/// Test builder for <see cref="RecipeTitle"/>. Use when a test needs a valid
/// <c>RecipeTitle</c> as a setup detail and doesn't care about the specific
/// value — <c>RecipeTitleBuilder.A().Build()</c> yields a sensible default.
///
/// VO builders intentionally have a much smaller surface than aggregate
/// builders: a single optional override and an implicit conversion so they
/// can be passed directly where a <c>RecipeTitle</c> is expected.
/// </summary>
public sealed class RecipeTitleBuilder
{
	private string value = "Test Recipe";

	public static RecipeTitleBuilder A() => new();

	public RecipeTitleBuilder With(string value)
	{
		this.value = value;
		return this;
	}

	public RecipeTitle Build() => RecipeTitle.From(value);

	public static implicit operator RecipeTitle(RecipeTitleBuilder builder) => builder.Build();
}
