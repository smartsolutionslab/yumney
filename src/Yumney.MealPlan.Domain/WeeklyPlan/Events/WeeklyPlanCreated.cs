using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan.Events;

public sealed record WeeklyPlanCreated(
	OwnerIdentifier Owner,
	WeekIdentifier Week,
	SlotServings DefaultServings) : DomainEvent;
