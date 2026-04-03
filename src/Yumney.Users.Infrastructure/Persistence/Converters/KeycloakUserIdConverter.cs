using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence.Converters;

internal sealed class KeycloakUserIdConverter()
    : ValueConverter<KeycloakUserId, string>(v => v.Value, v => KeycloakUserId.From(v));
