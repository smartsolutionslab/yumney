using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

public sealed record StepDescription
{
    public const int MaxLength = 2000;

    public string Value { get; }

    public StepDescription(string value)
    {
        string validated = Ensure.That(value)
            .IsNotNullOrWhiteSpace()
            .HasMaxLength(MaxLength)
            .AndReturn();
        Value = validated.Trim();
    }

    public static StepDescription From(string value) => new(value);

    public override string ToString() => Value;
}
