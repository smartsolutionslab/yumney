using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

public sealed record SearchTerm : IValueObject<string>
{
    public const int MaxLength = 200;

    public string Value { get; }

    private SearchTerm(string value)
    {
        string validated = Ensure.That(value)
            .IsNotNullOrWhiteSpace()
            .HasMaxLength(MaxLength)
            .AndReturn();
        Value = validated.Trim();
    }

    public static SearchTerm From(string value) => new(value);

    public static SearchTerm? FromNullable(string? value)
    {
        return value.HasValue() ? new SearchTerm(value!) : null;
    }

    public static implicit operator string(SearchTerm obj) => obj.Value;

    public override string ToString() => Value;
}
