using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

public sealed record PreferredLanguage : IValueObject<string>
{
    public const int MaxLength = 10;

    public static readonly PreferredLanguage English = new("en");
    public static readonly PreferredLanguage German = new("de");

    private static readonly string[] AllowedValues = [English, German];

    public string Value { get; }

    private PreferredLanguage(string value)
    {
        Value = Ensure.That(value)
            .IsNotNullOrWhiteSpace()
            .HasMaxLength(MaxLength)
            .AndReturn();
    }

    public static PreferredLanguage From(string value)
    {
        Ensure.That(value).IsOneOf(AllowedValues);
        return new(value);
    }

    public static implicit operator string(PreferredLanguage obj) => obj.Value;
}
