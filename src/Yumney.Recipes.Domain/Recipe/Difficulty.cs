using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

public sealed record Difficulty
{
    public const int MaxLength = 50;

    public string Value { get; }

    public Difficulty(string value)
    {
        string validated = Ensure.That(value)
            .IsNotNullOrWhiteSpace()
            .HasMaxLength(MaxLength)
            .AndReturn();
        Value = validated.Trim();
    }

    public static Difficulty? FromNullable(string? value) =>
        value.HasValue() ? new Difficulty(value!) : null;

    public override string ToString() => Value;
}
