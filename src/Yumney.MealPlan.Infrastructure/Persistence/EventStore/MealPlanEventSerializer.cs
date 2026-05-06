using System.Text.Json;
using System.Text.Json.Serialization;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan.Events;
using SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.Converters;
using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;
using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore.Json;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.EventStore;

internal static class MealPlanEventSerializer
{
#pragma warning disable SA1311
	public static JsonSerializerOptions Options { get; } = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		Converters =
		{
			new JsonStringEnumConverter(),
			new StringValueObjectJsonConverter<OwnerIdentifier>(OwnerIdentifier.From),
			new StringValueObjectJsonConverter<FreetextLabel>(FreetextLabel.From),
			new StringValueObjectJsonConverter<SlotRecipeTitle>(SlotRecipeTitle.From),
			new IntValueObjectJsonConverter<SlotServings>(SlotServings.From),
			new WeekIdentifierJsonConverter(),
			new SlotRecipeReferenceJsonConverter(),
		},
	};

	private static readonly Dictionary<string, Type> EventTypeMap = new()
	{
		[nameof(WeeklyPlanCreated)] = typeof(WeeklyPlanCreated),
		[nameof(ExtendedModeEnabled)] = typeof(ExtendedModeEnabled),
		[nameof(ExtendedModeDisabled)] = typeof(ExtendedModeDisabled),
		[nameof(RecipeAssigned)] = typeof(RecipeAssigned),
		[nameof(MealSetAsFreetext)] = typeof(MealSetAsFreetext),
		[nameof(LeftoverAssigned)] = typeof(LeftoverAssigned),
		[nameof(MealSlotCleared)] = typeof(MealSlotCleared),
		[nameof(ServingsAdjusted)] = typeof(ServingsAdjusted),
		[nameof(MealMarkedAsCooked)] = typeof(MealMarkedAsCooked),
		[nameof(MealMarkedAsSkipped)] = typeof(MealMarkedAsSkipped),
		[nameof(MealResetToPlanned)] = typeof(MealResetToPlanned),
		[nameof(MealSlotsSwapped)] = typeof(MealSlotsSwapped),
	};

	public static IEventSerializer Instance { get; } = new JsonEventSerializer(Options, EventTypeMap);
#pragma warning restore SA1311
}
