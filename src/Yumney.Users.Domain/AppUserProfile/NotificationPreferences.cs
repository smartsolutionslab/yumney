using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

public sealed record NotificationPreferences(
	bool TimerHapticFeedback,
	bool TimerSoundAlerts) : IValueObject
{
	public static readonly NotificationPreferences Default = new(true, true);
}
