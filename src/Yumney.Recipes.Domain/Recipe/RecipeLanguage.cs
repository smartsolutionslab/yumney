using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

public sealed record RecipeLanguage
{
    public const int MaxLength = 10;

    public string Value { get; }

    public RecipeLanguage(string value)
    {
        string validated = Ensure.That(value)
            .IsNotNullOrWhiteSpace()
            .HasMaxLength(MaxLength)
            .AndReturn();
        Value = validated.Trim().ToLowerInvariant();
    }

    public static RecipeLanguage? FromNullable(string? value) =>
        value.HasValue() ? new RecipeLanguage(value!) : null;

    public override string ToString() => Value;
}
