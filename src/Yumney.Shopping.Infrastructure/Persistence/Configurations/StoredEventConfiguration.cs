using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.Configurations;

internal sealed class StoredEventConfiguration()
	: StoredEventConfigurationBase<StoredEvent>("ShoppingEvents");
