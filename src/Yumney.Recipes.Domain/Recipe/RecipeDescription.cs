using Yumney.Shared.Guards;

namespace Yumney.Recipes.Domain.Recipe;

public sealed record RecipeDescription
{
    public const int MaxLength = 2000;

    public string Value { get; }

    public RecipeDescription(string value)
    {
        string validated = Ensure.That(value)
            .IsNotNullOrWhiteSpace()
            .HasMaxLength(MaxLength)
            .AndReturn();
        Value = validated.Trim();
    }

    public static RecipeDescription? FromNullable(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : new RecipeDescription(value);

    public override string ToString() => Value;
}
