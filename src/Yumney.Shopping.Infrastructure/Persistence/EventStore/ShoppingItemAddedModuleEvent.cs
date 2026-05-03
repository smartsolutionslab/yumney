using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger.Events;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;

#pragma warning disable SA1649
public sealed record ShoppingItemAddedModuleEvent(string OwnerId, ShoppingItemAdded Inner) : ModuleEvent(OwnerId);
