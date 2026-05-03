using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger.Events;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;

public sealed record ShoppingItemRemovedModuleEvent(string OwnerId, ShoppingItemRemoved Inner) : ModuleEvent(OwnerId);
