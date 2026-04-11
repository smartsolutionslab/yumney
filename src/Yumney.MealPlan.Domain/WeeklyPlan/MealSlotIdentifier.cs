using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

public sealed record MealSlotIdentifier : IValueObject
{
    public Guid Value { get; }

    private MealSlotIdentifier(Guid value)
    {
        Value = Ensure.That(value).IsNotEmpty().AndReturn();
    }

    public static MealSlotIdentifier New() => new(Guid.CreateVersion7());

    public static MealSlotIdentifier From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
