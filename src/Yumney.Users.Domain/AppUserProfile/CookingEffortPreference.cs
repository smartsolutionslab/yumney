using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

#pragma warning disable SA1311 // editorconfig requires camelCase for private fields
public sealed record CookingEffortPreference : IValueObject<string>
{
	public const int MaxLength = 25;

#pragma warning disable SA1202 // allowedValues must initialize before the public static instances
	private static readonly string[] allowedValues =
		["quick-weekdays", "balanced", "elaborate-weekends"];

	public static readonly CookingEffortPreference QuickWeekdays = new("quick-weekdays");
	public static readonly CookingEffortPreference Balanced = new("balanced");
	public static readonly CookingEffortPreference ElaborateWeekends = new("elaborate-weekends");
#pragma warning restore SA1202

	public string Value { get; }

	private CookingEffortPreference(string value)
	{
		Value = Ensure.That(value)
			.IsNotNullOrWhiteSpace()
			.HasMaxLength(MaxLength)
			.IsOneOf(allowedValues)
			.AndReturn();
	}

	public static CookingEffortPreference From(string value) => new(value);

	public static implicit operator string(CookingEffortPreference obj) => obj.Value;
}
#pragma warning restore SA1311
