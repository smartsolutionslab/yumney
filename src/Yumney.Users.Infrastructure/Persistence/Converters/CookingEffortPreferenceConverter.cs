using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence.Converters;

internal sealed class CookingEffortPreferenceConverter()
	: ValueConverter<CookingEffortPreference, string>(v => v.Value, v => CookingEffortPreference.From(v));
