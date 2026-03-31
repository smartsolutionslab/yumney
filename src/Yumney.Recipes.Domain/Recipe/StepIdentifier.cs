using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

public sealed record StepIdentifier : GuidIdentifier
{
    private StepIdentifier(Guid value)
        : base(value)
    {
    }

    public static StepIdentifier New() => new(Guid.NewGuid());

    public static StepIdentifier From(Guid value) => new(value);
}
