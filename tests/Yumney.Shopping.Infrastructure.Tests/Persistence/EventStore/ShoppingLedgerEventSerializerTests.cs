using System.Text.Json;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger.Events;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence.EventStore;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Infrastructure.Tests.Persistence.EventStore;

/// <summary>
/// Round-trips the value-object converters registered on the ShoppingLedger
/// serializer. Internal serializer surfaced to tests via InternalsVisibleTo.
/// </summary>
public class ShoppingLedgerEventSerializerTests
{
	[Fact]
	public void Options_RegistersConverters()
	{
		ShoppingLedgerEventSerializer.Options.Converters.Should().HaveCountGreaterThan(0);
	}

	[Fact]
	public void Roundtrip_ShoppingItemAdded_PreservesNameQuantityAndSource()
	{
		var name = ItemName.From("Onion");
		var quantity = Quantity.Of(Amount.From(3m), Unit.From("kg"));
		var source = ItemSource.From("manual");
		var @event = new ShoppingItemAdded(name, quantity, source);

		var json = JsonSerializer.Serialize(@event, ShoppingLedgerEventSerializer.Options);
		var rehydrated = (ShoppingItemAdded)ShoppingLedgerEventSerializer.Instance.Deserialize(
			nameof(ShoppingItemAdded), json)!;

		rehydrated.ItemName.Should().Be(name);
		rehydrated.Quantity.Amount.Value.Should().Be(3m);
		rehydrated.Quantity.Unit!.Value.Should().Be("kg");
		rehydrated.Source.Value.Should().Be("manual");
	}

	[Fact]
	public void Roundtrip_ShoppingItemRemoved_PreservesReason()
	{
		var @event = new ShoppingItemRemoved(
			ItemName.From("Bread"),
			Quantity.Of(Amount.From(1m), Unit.FromNullable(null)),
			RemovalReason.From("out of stock"));

		var json = JsonSerializer.Serialize(@event, ShoppingLedgerEventSerializer.Options);
		var rehydrated = (ShoppingItemRemoved)ShoppingLedgerEventSerializer.Instance.Deserialize(
			nameof(ShoppingItemRemoved), json)!;

		rehydrated.Reason!.Value.Should().Be("out of stock");
		rehydrated.Quantity.Unit.Should().BeNull();
	}

	[Fact]
	public void Roundtrip_ShoppingItemRemoved_NullReason_StaysNull()
	{
		var @event = new ShoppingItemRemoved(
			ItemName.From("Bread"),
			Quantity.Of(Amount.From(1m), Unit.From("loaf")),
			Reason: null);

		var json = JsonSerializer.Serialize(@event, ShoppingLedgerEventSerializer.Options);
		var rehydrated = (ShoppingItemRemoved)ShoppingLedgerEventSerializer.Instance.Deserialize(
			nameof(ShoppingItemRemoved), json)!;

		rehydrated.Reason.Should().BeNull();
	}

	[Fact]
	public void Deserialize_UnknownEventType_ReturnsNull()
	{
		ShoppingLedgerEventSerializer.Instance.Deserialize("NotALedgerEvent", "{}").Should().BeNull();
	}
}
