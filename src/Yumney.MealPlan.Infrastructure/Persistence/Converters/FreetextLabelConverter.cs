using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.Converters;

internal sealed class FreetextLabelConverter()
	: ValueConverter<FreetextLabel, string>(v => v.Value, v => FreetextLabel.From(v));
