using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan.Events;
using SmartSolutionsLab.Yumney.Shared.Events;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.EventStore;

#pragma warning disable SA1649
public sealed record MealSlotsSwappedIntegrationEvent(
	string OwnerId,
	string Week,
	MealSlotsSwapped Inner) : IntegrationEvent;
