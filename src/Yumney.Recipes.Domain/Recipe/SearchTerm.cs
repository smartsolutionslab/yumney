using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

public sealed record SearchTerm
{
    public const int MaxLength = 200;

    public string Value { get; }

    public SearchTerm(string value)
    {
        string validated = Ensure.That(value)
            .IsNotNullOrWhiteSpace()
            .HasMaxLength(MaxLength)
            .AndReturn();
        Value = validated.Trim();
    }

    public static SearchTerm? FromNullable(string? value)
    {
        return value.HasValue() ? new SearchTerm(value!) : null;
    }

    public override string ToString() => Value;
}
