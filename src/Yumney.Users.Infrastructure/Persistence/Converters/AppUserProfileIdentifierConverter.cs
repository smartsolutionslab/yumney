using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence.Converters;

internal sealed class AppUserProfileIdentifierConverter()
	: ValueConverter<AppUserProfileIdentifier, Guid>(v => v.Value, v => AppUserProfileIdentifier.From(v));
