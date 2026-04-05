using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartSolutionsLab.Yumney.Users.Domain.UserActivity;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence.Converters;

internal sealed class UserActivityOwnerIdentifierConverter()
    : ValueConverter<OwnerIdentifier, string>(v => v.Value, v => OwnerIdentifier.From(v));
