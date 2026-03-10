using Yumney.Shared.Guards;

namespace Yumney.Recipes.Domain.Recipe;

public sealed record Difficulty
{
    public const int MaxLength = 50;

    public string Value { get; }

    public Difficulty(string value)
    {
        string validated = Ensure.That(value)
            .IsNotNullOrWhiteSpace()
            .HasMaxLength(MaxLength)
            .AndReturn();
        Value = validated.Trim();
    }

    public static Difficulty? FromNullable(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : new Difficulty(value);

    public override string ToString() => Value;
}
