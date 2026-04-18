using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.Converters;

internal sealed class MealSlotIdentifierConverter()
	: ValueConverter<MealSlotIdentifier, Guid>(v => v.Value, v => MealSlotIdentifier.From(v));
