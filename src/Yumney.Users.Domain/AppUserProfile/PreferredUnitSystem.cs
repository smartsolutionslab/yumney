using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

public sealed record PreferredUnitSystem : IValueObject<string>
{
    public const int MaxLength = 20;

    public string Value { get; }

    private PreferredUnitSystem(string value)
    {
        Value = Ensure.That(value)
            .IsNotNullOrWhiteSpace()
            .HasMaxLength(MaxLength)
            .AndReturn();
    }

    public static PreferredUnitSystem From(string value) => new(value);

    public static implicit operator string(PreferredUnitSystem obj) => obj.Value;
}
