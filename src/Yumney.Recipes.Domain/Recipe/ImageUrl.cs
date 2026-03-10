using Yumney.Shared.Guards;

namespace Yumney.Recipes.Domain.Recipe;

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

    public override string ToString() => Value;
}
