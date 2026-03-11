using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

public sealed record DisplayName
{
    public string Value { get; }

    public DisplayName(string value)
    {
        string validated = Ensure.That(value)
            .IsNotNullOrWhiteSpace()
            .HasMaxLength(200)
            .AndReturn();
        Value = validated.Trim();
    }

    public override string ToString() => Value;
}
