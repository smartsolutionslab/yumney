using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger.Events;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore.Events;

public sealed record ShoppingItemQuantityAdjustedModuleEvent(
	string OwnerId,
	ShoppingItemQuantityAdjusted Inner) : ModuleEvent(OwnerId);
