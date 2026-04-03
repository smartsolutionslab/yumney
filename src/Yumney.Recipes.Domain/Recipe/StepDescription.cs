using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

public sealed record StepDescription : IValueObject
{
    public const int MaxLength = 2000;

    public string Value { get; }

    private StepDescription(string value)
    {
        string validated = Ensure.That(value)
            .IsNotNullOrWhiteSpace()
            .HasMaxLength(MaxLength)
            .AndReturn();
        Value = validated.Trim();
    }

    public static StepDescription From(string value) => new(value);

    public static implicit operator string(StepDescription obj) => obj.Value;
}
