using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartSolutionsLab.Yumney.Users.Domain.UserActivity;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence.Converters;

internal sealed class ActivityTypeConverter()
    : ValueConverter<ActivityType, string>(v => v.Value, v => ActivityType.From(v));
