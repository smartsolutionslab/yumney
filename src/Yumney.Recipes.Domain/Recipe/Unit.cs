using Yumney.Shared.Guards;

namespace Yumney.Recipes.Domain.Recipe;

public sealed record Unit
{
    public const int MaxLength = 50;

    public string Value { get; }

    public Unit(string value)
    {
        string validated = Ensure.That(value)
            .IsNotNullOrWhiteSpace()
            .HasMaxLength(MaxLength)
            .AndReturn();
        Value = validated.Trim();
    }

    public override string ToString() => Value;
}
