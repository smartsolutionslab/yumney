using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

public sealed record RecipeTag : IValueObject
{
    public const int MaxLength = 50;

    public string Value { get; }

    private RecipeTag(string value)
    {
        string validated = Ensure.That(value)
            .IsNotNullOrWhiteSpace()
            .HasMaxLength(MaxLength)
            .AndReturn();
        Value = validated.Trim().ToLowerInvariant();
    }

    public static RecipeTag From(string value) => new(value);

    public static implicit operator string(RecipeTag obj) => obj.Value;

    public override string ToString() => Value;
}
