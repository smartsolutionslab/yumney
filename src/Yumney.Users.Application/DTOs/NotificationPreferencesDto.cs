namespace SmartSolutionsLab.Yumney.Users.Application.DTOs;

public sealed record NotificationPreferencesDto(
	bool TimerHapticFeedback,
	bool TimerSoundAlerts);
