using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger.Events;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;

#pragma warning disable SA1649
public sealed record ShoppingItemAddedIntegrationEvent(string OwnerId, ShoppingItemAdded Inner) : IntegrationEvent;

public sealed record ShoppingItemBoughtIntegrationEvent(string OwnerId, ShoppingItemBought Inner) : IntegrationEvent;

public sealed record ShoppingItemConsumedIntegrationEvent(string OwnerId, ShoppingItemConsumed Inner) : IntegrationEvent;

public sealed record ShoppingItemRemovedIntegrationEvent(string OwnerId, ShoppingItemRemoved Inner) : IntegrationEvent;

public sealed record ShoppingItemQuantityAdjustedIntegrationEvent(string OwnerId, ShoppingItemQuantityAdjusted Inner) : IntegrationEvent;
