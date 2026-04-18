using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence.Converters;

internal sealed class DefaultServingsConverter()
	: ValueConverter<DefaultServings, int>(v => v.Value, v => DefaultServings.From(v));
