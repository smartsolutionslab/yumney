using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger.Events;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;

#pragma warning disable SA1649

public sealed record ShoppingItemAddedIntegrationEvent(ShoppingItemAdded Inner) : IntegrationEvent;

public sealed record ShoppingItemBoughtIntegrationEvent(ShoppingItemBought Inner) : IntegrationEvent;

public sealed record ShoppingItemConsumedIntegrationEvent(ShoppingItemConsumed Inner) : IntegrationEvent;

public sealed record ShoppingItemRemovedIntegrationEvent(ShoppingItemRemoved Inner) : IntegrationEvent;

public sealed record ShoppingItemQuantityAdjustedIntegrationEvent(ShoppingItemQuantityAdjusted Inner) : IntegrationEvent;
