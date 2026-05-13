using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Builders;

/// <summary>
/// Test builder for the <see cref="Step"/> entity. Defaults to step number 1
/// and the default StepDescription ("Mix ingredients").
/// </summary>
public sealed class StepBuilder
{
	private int number = 1;
	private StepDescription description = StepDescription.From("Mix ingredients");

	public static StepBuilder A() => new();

	public StepBuilder Numbered(int value)
	{
		number = value;
		return this;
	}

	public StepBuilder WithDescription(string value)
	{
		description = StepDescription.From(value);
		return this;
	}

	public StepBuilder WithDescription(StepDescription value)
	{
		description = value;
		return this;
	}

	public Step Build() => Step.Create(StepNumber.From(number), description);

	public static implicit operator Step(StepBuilder builder) => builder.Build();
}
