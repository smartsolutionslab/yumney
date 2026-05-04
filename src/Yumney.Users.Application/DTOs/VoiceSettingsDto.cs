namespace SmartSolutionsLab.Yumney.Users.Application.DTOs;

public sealed record VoiceSettingsDto(
	bool Enabled,
	string Speed,
	bool AutoReadInCookMode);
