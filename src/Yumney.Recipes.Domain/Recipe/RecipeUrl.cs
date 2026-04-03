using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

public sealed record RecipeUrl : IValueObject<string>
{
    public const int MaxLength = 2048;

    public string Value { get; }

    private RecipeUrl(string value)
    {
        string validated = Ensure.That(value)
            .IsNotNullOrWhiteSpace()
            .HasMaxLength(MaxLength)
            .IsValidUrl()
            .AndReturn();
        Value = validated.Trim();
    }

    public static RecipeUrl From(string value) => new(value);

    public static RecipeUrl? FromNullable(string? value) =>
        value.HasValue() ? new RecipeUrl(value!) : null;

    public static implicit operator string(RecipeUrl obj) => obj.Value;
}
