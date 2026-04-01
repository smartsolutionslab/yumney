using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

public sealed record RecipeLanguage : IValueObject
{
    public const int MaxLength = 10;

    public string Value { get; }

    private RecipeLanguage(string value)
    {
        string validated = Ensure.That(value)
            .IsNotNullOrWhiteSpace()
            .HasMaxLength(MaxLength)
            .AndReturn();
        Value = validated.Trim().ToLowerInvariant();
    }

    public static RecipeLanguage From(string value) => new(value);

    public static RecipeLanguage? FromNullable(string? value) =>
        value.HasValue() ? new RecipeLanguage(value!) : null;

    public static explicit operator string(RecipeLanguage obj) => obj.Value;

    public override string ToString() => Value;
}
