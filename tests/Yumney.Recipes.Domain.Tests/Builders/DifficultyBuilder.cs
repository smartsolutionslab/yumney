using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Builders;

public sealed class DifficultyBuilder
{
	private string value = "Easy";

	public static DifficultyBuilder A() => new();

	public DifficultyBuilder With(string value)
	{
		this.value = value;
		return this;
	}

	public Difficulty Build() => Difficulty.From(value);

	public static implicit operator Difficulty(DifficultyBuilder builder) => builder.Build();
}
