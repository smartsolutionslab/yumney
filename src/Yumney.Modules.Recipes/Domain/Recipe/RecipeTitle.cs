using Yumney.Shared.Guards;

namespace Yumney.Modules.Recipes.Domain.Recipe;

public record RecipeTitle
{
    public string Value { get; }

    public RecipeTitle(string value)
    {
        Value = Ensure.That(value)
            .IsNotNullOrWhiteSpace()
            .HasMaxLength(200)
            .AndReturn()
            .Trim();
    }

    public static implicit operator string(RecipeTitle title) => title.Value;
}
