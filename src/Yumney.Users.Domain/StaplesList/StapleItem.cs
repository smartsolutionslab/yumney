using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Users.Domain.StaplesList;

public sealed record StapleItem : IValueObject<string>
{
    public const int MaxLength = 100;

    public string Value { get; }

    private StapleItem(string value)
    {
        string validated = Ensure.That(value)
            .IsNotNullOrWhiteSpace()
            .HasMaxLength(MaxLength)
            .AndReturn();
        Value = validated.Trim().ToLowerInvariant();
    }

    public static StapleItem From(string value) => new(value);

    public static implicit operator string(StapleItem obj) => obj.Value;
}
