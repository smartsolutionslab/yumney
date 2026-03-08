using Yumney.Shared.Guards;

namespace Yumney.Recipes.Domain.Recipe;

public sealed record RecipeUrl
{
    public string Value { get; }

    public RecipeUrl(string value)
    {
        string validated = Ensure.That(value)
            .IsNotNullOrWhiteSpace()
            .HasMaxLength(2048)
            .IsValidUrl()
            .AndReturn();
        Value = validated.Trim();
    }

    public override string ToString() => Value;
}
