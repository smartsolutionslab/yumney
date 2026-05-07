using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger.Events;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore.Events;

public sealed record ShoppingItemMarkedAsFrozenModuleEvent(string OwnerId, ShoppingItemMarkedAsFrozen Inner) : ModuleEvent(OwnerId);
