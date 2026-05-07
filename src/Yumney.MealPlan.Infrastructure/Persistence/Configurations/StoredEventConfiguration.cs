using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Persistence.Configurations;

internal sealed class StoredEventConfiguration() : StoredEventConfigurationBase<StoredEvent>("MealPlanEvents");
