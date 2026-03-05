using Yumney.Shared.Guards;

namespace Yumney.Modules.Recipes.Domain.Recipe;

public record RecipeDescription
{
    public string Value { get; }

    public RecipeDescription(string value)
    {
        Value = Ensure.That(value)
            .IsNotNullOrWhiteSpace()
            .HasMaxLength(5000)
            .AndReturn()
            .Trim();
    }

    public static implicit operator string(RecipeDescription desc) => desc.Value;
}
