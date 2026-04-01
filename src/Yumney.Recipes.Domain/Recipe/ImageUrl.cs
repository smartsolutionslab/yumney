using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

public sealed record ImageUrl : IValueObject
{
    public const int MaxLength = 2048;

    public string Value { get; }

    private ImageUrl(string value)
    {
        string validated = Ensure.That(value)
            .IsNotNullOrWhiteSpace()
            .HasMaxLength(MaxLength)
            .IsValidUrl()
            .AndReturn();
        Value = validated.Trim();
    }

    public static ImageUrl From(string value) => new(value);

    public static ImageUrl? FromNullable(string? value) =>
        value.HasValue() ? new ImageUrl(value!) : null;

    public static implicit operator string(ImageUrl obj) => obj.Value;

    public override string ToString() => Value;
}
