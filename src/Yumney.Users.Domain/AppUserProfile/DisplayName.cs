using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

public sealed record DisplayName : IValueObject
{
    public const int MaxLength = 200;

    public string Value { get; }

    private DisplayName(string value)
    {
        string validated = Ensure.That(value)
            .IsNotNullOrWhiteSpace()
            .HasMaxLength(MaxLength)
            .AndReturn();
        Value = validated.Trim();
    }

    public static DisplayName From(string value) => new(value);

    public static explicit operator string(DisplayName obj) => obj.Value;

    public override string ToString() => Value;
}
