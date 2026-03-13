using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

public sealed record Password
{
    public const int MinLength = 8;
    public const string UppercasePattern = "[A-Z]";
    public const string LowercasePattern = "[a-z]";
    public const string DigitPattern = "[0-9]";
    public const string UppercaseMessage = "Password must contain at least one uppercase letter.";
    public const string LowercaseMessage = "Password must contain at least one lowercase letter.";
    public const string DigitMessage = "Password must contain at least one digit.";

    public string Value { get; }

    public Password(string value)
    {
        Value = Ensure.That(value)
            .IsNotNullOrWhiteSpace()
            .HasMinLength(MinLength)
            .Matches(UppercasePattern, UppercaseMessage)
            .Matches(LowercasePattern, LowercaseMessage)
            .Matches(DigitPattern, DigitMessage)
            .AndReturn();
    }

    public override string ToString() => "***";
}
