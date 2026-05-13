using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Builders;

public sealed class CookingTimeBuilder
{
	private int value = 30;

	public static CookingTimeBuilder A() => new();

	public CookingTimeBuilder With(int value)
	{
		this.value = value;
		return this;
	}

	public CookingTime Build() => CookingTime.From(value);

	public static implicit operator CookingTime(CookingTimeBuilder builder) => builder.Build();
}
