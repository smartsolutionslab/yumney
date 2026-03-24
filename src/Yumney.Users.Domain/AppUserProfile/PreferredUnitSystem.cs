using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

public sealed record PreferredUnitSystem
{
    public const int MaxLength = 20;

    public string Value { get; }

    public PreferredUnitSystem(string value)
    {
        Value = Ensure.That(value)
            .IsNotNullOrWhiteSpace()
            .HasMaxLength(MaxLength)
            .AndReturn();
    }

    public static PreferredUnitSystem From(string value) => new(value);

    public override string ToString() => Value;
}
