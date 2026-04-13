using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

public sealed record Difficulty : IValueObject<string>
{
    public const int MaxLength = 50;

    public static readonly Difficulty Easy = new("Easy");
    public static readonly Difficulty Medium = new("Medium");
    public static readonly Difficulty Hard = new("Hard");

    private static readonly IReadOnlyList<Difficulty> KnownValues = [Easy, Medium, Hard];

    public string Value { get; }

    private Difficulty(string value)
    {
        string validated = Ensure.That(value)
            .IsNotNullOrWhiteSpace()
            .HasMaxLength(MaxLength)
            .AndReturn();
        Value = validated.Trim();
    }

    public static Difficulty From(string value)
    {
        var trimmed = new Difficulty(value);
        return KnownValues.FirstOrDefault(
            k => string.Equals(k.Value, trimmed.Value, StringComparison.OrdinalIgnoreCase))
            ?? throw new GuardException(
                nameof(value),
                $"Difficulty must be one of: {string.Join(", ", AllValues)}.");
    }

    public static Difficulty? FromNullable(string? value) =>
        value.HasValue() ? From(value!) : null;

    public static IReadOnlyList<string> AllValues => KnownValues.Select(d => d.Value).ToList();

    public static implicit operator string(Difficulty obj) => obj.Value;
}
