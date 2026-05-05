using SmartSolutionsLab.Yumney.Shared.Abstractions;

namespace SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

public sealed record VoiceSettings(
	bool Enabled,
	VoiceSpeed Speed,
	bool AutoReadInCookMode) : IValueObject
{
	public static readonly VoiceSettings Default = new(true, VoiceSpeed.Normal, false);
}
