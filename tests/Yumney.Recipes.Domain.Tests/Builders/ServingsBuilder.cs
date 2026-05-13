using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Builders;

public sealed class ServingsBuilder
{
	private int value = 4;

	public static ServingsBuilder A() => new();

	public ServingsBuilder With(int value)
	{
		this.value = value;
		return this;
	}

	public Servings Build() => Servings.From(value);

	public static implicit operator Servings(ServingsBuilder builder) => builder.Build();
}
