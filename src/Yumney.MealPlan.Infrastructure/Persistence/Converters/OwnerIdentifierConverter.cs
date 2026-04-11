using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.Converters;

internal sealed class OwnerIdentifierConverter()
    : ValueConverter<OwnerIdentifier, string>(v => v.Value, v => OwnerIdentifier.From(v));
