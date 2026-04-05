using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartSolutionsLab.Yumney.Users.Domain.UserActivity;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence.Converters;

internal sealed class UserActivityIdentifierConverter()
    : ValueConverter<UserActivityIdentifier, Guid>(v => v.Value, v => UserActivityIdentifier.From(v));
