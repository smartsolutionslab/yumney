using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

public sealed record PreferredLanguage : IValueObject<string>
{
    public const int MaxLength = 10;

#pragma warning disable SA1202 // AllowedValues must initialize before the public static instances
    private static readonly string[] AllowedValues = ["en", "de"];

    public static readonly PreferredLanguage English = new("en");
    public static readonly PreferredLanguage German = new("de");
#pragma warning restore SA1202

    public string Value { get; }

    private PreferredLanguage(string value)
    {
        Value = Ensure.That(value)
            .IsNotNullOrWhiteSpace()
            .HasMaxLength(MaxLength)
            .IsOneOf(AllowedValues)
            .AndReturn();
    }

    public static PreferredLanguage From(string value) => new(value);

    public static implicit operator string(PreferredLanguage obj) => obj.Value;
}
