using Yumney.Shared.Guards;

namespace Yumney.Recipes.Domain.Recipe;

public sealed record RecipeTitle
{
    public const int MaxLength = 200;

    public string Value { get; }

    public RecipeTitle(string value)
    {
        string validated = Ensure.That(value)
            .IsNotNullOrWhiteSpace()
            .HasMaxLength(MaxLength)
            .AndReturn();
        Value = validated.Trim();
    }

    public override string ToString() => Value;
}
