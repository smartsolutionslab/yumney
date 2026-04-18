using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence.Converters;

internal sealed class PreferredLanguageConverter()
	: ValueConverter<PreferredLanguage, string>(v => v.Value, v => PreferredLanguage.From(v));
