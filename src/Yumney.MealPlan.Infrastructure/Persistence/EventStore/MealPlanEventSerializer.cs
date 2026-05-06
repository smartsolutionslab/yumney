using System.Text.Json;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan.Events;
using SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.Converters;
using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;
using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore.Json;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.EventStore;

internal static class MealPlanEventSerializer
{
#pragma warning disable SA1311
	public static JsonSerializerOptions Options { get; } = BuildOptions();

	public static IEventSerializer Instance { get; } = new JsonEventSerializer(
		Options,
		EventTypeRegistry.BuildFromAssembly(
			typeof(WeeklyPlanCreated).Assembly,
			type => type.Namespace == typeof(WeeklyPlanCreated).Namespace));
#pragma warning restore SA1311

	private static JsonSerializerOptions BuildOptions()
	{
		var options = EventSerializerDefaults.Options();
		options.Converters.Add(new StringValueObjectJsonConverter<OwnerIdentifier>(OwnerIdentifier.From));
		options.Converters.Add(new StringValueObjectJsonConverter<FreetextLabel>(FreetextLabel.From));
		options.Converters.Add(new StringValueObjectJsonConverter<SlotRecipeTitle>(SlotRecipeTitle.From));
		options.Converters.Add(new IntValueObjectJsonConverter<SlotServings>(SlotServings.From));
		options.Converters.Add(new WeekIdentifierJsonConverter());
		options.Converters.Add(new SlotRecipeReferenceJsonConverter());
		return options;
	}
}
