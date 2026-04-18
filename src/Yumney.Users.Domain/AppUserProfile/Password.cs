using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

public sealed record Password : IValueObject
{
	public const int MinLength = 8;
	public const string UppercasePattern = "[A-Z]";
	public const string LowercasePattern = "[a-z]";
	public const string DigitPattern = "[0-9]";
	public const string UppercaseMessage = "Password must contain at least one uppercase letter.";
	public const string LowercaseMessage = "Password must contain at least one lowercase letter.";
	public const string DigitMessage = "Password must contain at least one digit.";

	public string Value { get; }

	private Password(string value)
	{
		Value = Ensure.That(value)
			.IsNotNullOrWhiteSpace()
			.HasMinLength(MinLength)
			.Matches(UppercasePattern, UppercaseMessage)
			.Matches(LowercasePattern, LowercaseMessage)
			.Matches(DigitPattern, DigitMessage)
			.AndReturn();
	}

	public static Password From(string value) => new(value);

	public static implicit operator string(Password obj) => obj.Value;

	public override string ToString() => "***";
}
