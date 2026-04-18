using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

public sealed record StepIdentifier : IValueObject
{
	public Guid Value { get; }

	private StepIdentifier(Guid value)
	{
		Value = Ensure.That(value).IsNotEmpty().AndReturn();
	}

	public static StepIdentifier New() => new(Guid.CreateVersion7());

	public static StepIdentifier From(Guid value) => new(value);

	public override string ToString() => Value.ToString();
}
