using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

public sealed record Email : IValueObject
{
    public const int MaxLength = 254;

    public string Value { get; }

    private Email(string value)
    {
        string validated = Ensure.That(value)
            .IsNotNullOrWhiteSpace()
            .HasMaxLength(MaxLength)
            .IsValidEmail()
            .AndReturn();
        Value = validated.Trim().ToLowerInvariant();
    }

    public static Email From(string value) => new(value);

    public static explicit operator string(Email obj) => obj.Value;

    public override string ToString() => Value;
}
