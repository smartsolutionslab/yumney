using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger.Events;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore.Events;

public sealed record ShoppingItemBoughtModuleEvent(string OwnerId, ShoppingItemBought Inner) : ModuleEvent(OwnerId);
