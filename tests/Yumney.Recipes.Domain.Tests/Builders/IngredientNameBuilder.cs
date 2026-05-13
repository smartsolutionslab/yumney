using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Builders;

public sealed class IngredientNameBuilder
{
	private string value = "Flour";

	public static IngredientNameBuilder A() => new();

	public IngredientNameBuilder With(string value)
	{
		this.value = value;
		return this;
	}

	public IngredientName Build() => IngredientName.From(value);

	public static implicit operator IngredientName(IngredientNameBuilder builder) => builder.Build();
}
