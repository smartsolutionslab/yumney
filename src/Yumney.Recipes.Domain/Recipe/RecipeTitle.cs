using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

public sealed record RecipeTitle
{
    public const int MaxLength = 200;

    public string Value { get; }

    private RecipeTitle(string value)
    {
        string validated = Ensure.That(value)
            .IsNotNullOrWhiteSpace()
            .HasMaxLength(MaxLength)
            .AndReturn();
        Value = validated.Trim();
    }

    public static RecipeTitle From(string value) => new(value);

    public override string ToString() => Value;
}
