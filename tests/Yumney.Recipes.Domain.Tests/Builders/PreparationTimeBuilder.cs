using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Builders;

public sealed class PreparationTimeBuilder
{
	private int value = 15;

	public static PreparationTimeBuilder A() => new();

	public PreparationTimeBuilder With(int value)
	{
		this.value = value;
		return this;
	}

	public PreparationTime Build() => PreparationTime.From(value);

	public static implicit operator PreparationTime(PreparationTimeBuilder builder) => builder.Build();
}
