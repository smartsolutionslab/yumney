using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

public sealed record Difficulty : IValueObject
{
    public const int MaxLength = 50;

    public string Value { get; }

    private Difficulty(string value)
    {
        string validated = Ensure.That(value)
            .IsNotNullOrWhiteSpace()
            .HasMaxLength(MaxLength)
            .AndReturn();
        Value = validated.Trim();
    }

    public static Difficulty From(string value) => new(value);

    public static Difficulty? FromNullable(string? value) =>
        value.HasValue() ? new Difficulty(value!) : null;

    public static implicit operator string(Difficulty obj) => obj.Value;

    public override string ToString() => Value;
}
