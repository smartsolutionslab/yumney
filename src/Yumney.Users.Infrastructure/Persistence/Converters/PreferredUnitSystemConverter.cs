using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence.Converters;

internal sealed class PreferredUnitSystemConverter()
	: ValueConverter<PreferredUnitSystem, string>(v => v.Value, v => PreferredUnitSystem.From(v));
