using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence.Converters;

internal sealed class VoiceSpeedConverter()
	: ValueConverter<VoiceSpeed, string>(v => v.Value, v => VoiceSpeed.From(v));
