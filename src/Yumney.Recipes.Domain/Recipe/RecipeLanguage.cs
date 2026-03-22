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
        string.IsNullOrWhiteSpace(value) ? null : new RecipeLanguage(value);

    public override string ToString() => Value;
}
