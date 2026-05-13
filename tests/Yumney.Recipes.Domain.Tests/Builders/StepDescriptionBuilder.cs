using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Builders;

public sealed class StepDescriptionBuilder
{
	private string value = "Mix ingredients";

	public static StepDescriptionBuilder A() => new();

	public StepDescriptionBuilder With(string value)
	{
		this.value = value;
		return this;
	}

	public StepDescription Build() => StepDescription.From(value);

	public static implicit operator StepDescription(StepDescriptionBuilder builder) => builder.Build();
}
