using Yumney.Shared.Guards;

namespace Yumney.Recipes.Domain.Recipe;

public sealed record OwnerIdentifier
{
    public const int MaxLength = 255;

    public string Value { get; }

    public OwnerIdentifier(string value)
    {
        string validated = Ensure.That(value)
            .IsNotNullOrWhiteSpace()
            .HasMaxLength(MaxLength)
            .AndReturn();
        Value = validated.Trim();
    }

    public override string ToString() => Value;
}
