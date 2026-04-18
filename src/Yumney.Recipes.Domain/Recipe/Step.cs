using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

public sealed class Step : Entity<StepIdentifier>
{
	public StepNumber Number { get; private set; } = default!;

	public StepDescription Description { get; private set; } = default!;

	private Step()
	{
	}

	public static Step Create(StepNumber number, StepDescription description)
	{
		return new Step
		{
			Id = StepIdentifier.New(),
			Number = number,
			Description = description,
		};
	}
}
