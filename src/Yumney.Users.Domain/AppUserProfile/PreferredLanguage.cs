using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

public sealed record PreferredLanguage : IValueObject<string>
{
    public const int MaxLength = 10;

    public string Value { get; }

    private PreferredLanguage(string value)
    {
        Value = Ensure.That(value)
            .IsNotNullOrWhiteSpace()
            .HasMaxLength(MaxLength)
            .AndReturn();
    }

    public static PreferredLanguage From(string value) => new(value);

    public static implicit operator string(PreferredLanguage obj) => obj.Value;

    public override string ToString() => Value;
}
