using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.Converters;

internal sealed class WeeklyPlanIdentifierConverter()
    : ValueConverter<WeeklyPlanIdentifier, Guid>(v => v.Value, v => WeeklyPlanIdentifier.From(v));
