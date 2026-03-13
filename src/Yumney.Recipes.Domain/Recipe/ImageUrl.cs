using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

public sealed record ImageUrl
{
    public const int MaxLength = 2048;

    public string Value { get; }

    public ImageUrl(string value)
    {
        string validated = Ensure.That(value)
            .IsNotNullOrWhiteSpace()
            .HasMaxLength(MaxLength)
            .IsValidUrl()
            .AndReturn();
        Value = validated.Trim();
    }

    public static ImageUrl? FromNullable(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : new ImageUrl(value);

    public override string ToString() => Value;
}
