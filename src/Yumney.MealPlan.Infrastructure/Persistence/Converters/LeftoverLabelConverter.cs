using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.Converters;

internal sealed class LeftoverLabelConverter()
	: ValueConverter<LeftoverLabel, string>(v => v.Value, v => LeftoverLabel.From(v));
