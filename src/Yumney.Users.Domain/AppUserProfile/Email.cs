using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

public sealed record Email
{
    public const int MaxLength = 254;

    public string Value { get; }

    public Email(string value)
    {
        string validated = Ensure.That(value)
            .IsNotNullOrWhiteSpace()
            .HasMaxLength(MaxLength)
            .IsValidEmail()
            .AndReturn();
        Value = validated.Trim().ToLowerInvariant();
    }

    public override string ToString() => Value;
}
