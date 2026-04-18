using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence.Converters;

internal sealed class DisplayNameConverter()
	: ValueConverter<DisplayName, string>(v => v.Value, v => DisplayName.From(v));
