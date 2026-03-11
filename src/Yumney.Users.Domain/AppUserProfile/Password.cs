using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

public sealed record Password
{
    public string Value { get; }

    public Password(string value)
    {
        Value = Ensure.That(value)
            .IsNotNullOrWhiteSpace()
            .HasMinLength(8)
            .Matches("[A-Z]", "Password must contain at least one uppercase letter.")
            .Matches("[a-z]", "Password must contain at least one lowercase letter.")
            .Matches("[0-9]", "Password must contain at least one digit.")
            .AndReturn();
    }

    public override string ToString() => "***";
}
