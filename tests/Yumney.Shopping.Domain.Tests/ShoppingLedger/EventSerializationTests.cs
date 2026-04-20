using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger.Events;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.Tests.ShoppingLedger;

public class EventSerializationTests
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		Converters =
		{
			new ItemNameJsonConverter(),
			new AmountJsonConverter(),
			new UnitJsonConverter(),
			new RemovalReasonJsonConverter(),
			new ItemSourceJsonConverter(),
		},
	};

	[Fact]
	public void ShoppingItemAdded_RoundTrip_PreservesValues()
	{
		var milk = ItemName.From("Milk");
		var amount = Amount.From(2.5m);
		var unit = Unit.From("L");
		var source = ItemSource.Manual;
		var original = new ShoppingItemAdded(milk, Quantity.Of(amount, unit), source);

		var json = JsonSerializer.Serialize(original, JsonOptions);
		var deserialized = JsonSerializer.Deserialize<ShoppingItemAdded>(json, JsonOptions)!;

		deserialized.ItemName.Should().Be(milk);
		deserialized.Quantity.Amount.Should().Be(amount);
		deserialized.Quantity.Unit.Should().Be(unit);
		deserialized.Source.Should().Be(source);
	}

	[Fact]
	public void ShoppingItemAdded_SerializesAsNestedQuantity()
	{
		var eggs = ItemName.From("Eggs");
		var amount = Amount.From(6);
		var unit = Unit.From("pc");
		var source = ItemSource.From("recipe:abc");
		var @event = new ShoppingItemAdded(eggs, Quantity.Of(amount, unit), source);

		var json = JsonSerializer.Serialize(@event, JsonOptions);

		json.Should().Contain("\"itemName\":\"Eggs\"");
		json.Should().Contain("\"amount\":6");
		json.Should().Contain("\"unit\":\"pc\"");
		json.Should().Contain("\"source\":\"recipe:abc\"");
	}

	[Fact]
	public void ShoppingItemBought_WithNullUnit_RoundTrips()
	{
		var banana = ItemName.From("Banana");
		var amount = Amount.From(3);
		var original = new ShoppingItemBought(banana, Quantity.Of(amount, null));

		var json = JsonSerializer.Serialize(original, JsonOptions);
		var deserialized = JsonSerializer.Deserialize<ShoppingItemBought>(json, JsonOptions)!;

		deserialized.ItemName.Should().Be(banana);
		deserialized.Quantity.Amount.Should().Be(amount);
		deserialized.Quantity.Unit.Should().BeNull();
	}

	[Fact]
	public void ShoppingItemRemoved_RoundTrip_PreservesReason()
	{
		var reason = RemovalReason.From("spoiled");
		var original = new ShoppingItemRemoved(
			ItemName.From("Milk"),
			Quantity.Of(Amount.From(1), Unit.From("L")),
			reason);

		var json = JsonSerializer.Serialize(original, JsonOptions);
		var deserialized = JsonSerializer.Deserialize<ShoppingItemRemoved>(json, JsonOptions)!;

		deserialized.Reason.Should().Be(reason);
	}

	[Fact]
	public void ShoppingItemQuantityAdjusted_RoundTrip()
	{
		var amount = Amount.From(1.5m);
		var unit = Unit.From("kg");
		var original = new ShoppingItemQuantityAdjusted(
			ItemName.From("Rice"),
			Quantity.Of(amount, unit));

		var json = JsonSerializer.Serialize(original, JsonOptions);
		var deserialized = JsonSerializer.Deserialize<ShoppingItemQuantityAdjusted>(json, JsonOptions)!;

		deserialized.NewQuantity.Amount.Should().Be(amount);
		deserialized.NewQuantity.Unit.Should().Be(unit);
	}

	[Fact]
	public void ShoppingModeStarted_RoundTrip_NotAffectedByConverters()
	{
		var timestamp = new DateTime(2026, 4, 15, 10, 30, 0, DateTimeKind.Utc);
		var original = new ShoppingModeStarted(timestamp);

		var json = JsonSerializer.Serialize(original, JsonOptions);
		var deserialized = JsonSerializer.Deserialize<ShoppingModeStarted>(json, JsonOptions)!;

		deserialized.SnapshotTakenAt.Should().Be(timestamp);
	}

	private sealed class ItemNameJsonConverter : JsonConverter<ItemName>
	{
		public override ItemName Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
			ItemName.From(reader.GetString()!);

		public override void Write(Utf8JsonWriter writer, ItemName value, JsonSerializerOptions options) =>
			writer.WriteStringValue(value.Value);
	}

	private sealed class AmountJsonConverter : JsonConverter<Amount>
	{
		public override Amount Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
			Amount.From(reader.GetDecimal());

		public override void Write(Utf8JsonWriter writer, Amount value, JsonSerializerOptions options) =>
			writer.WriteNumberValue(value.Value);
	}

	private sealed class UnitJsonConverter : JsonConverter<Unit>
	{
		public override Unit? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			var value = reader.GetString();
			return value is not null ? Unit.From(value) : null;
		}

		public override void Write(Utf8JsonWriter writer, Unit value, JsonSerializerOptions options) =>
			writer.WriteStringValue(value.Value);
	}

	private sealed class RemovalReasonJsonConverter : JsonConverter<RemovalReason>
	{
		public override RemovalReason? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			var value = reader.GetString();
			return value is not null ? RemovalReason.From(value) : null;
		}

		public override void Write(Utf8JsonWriter writer, RemovalReason value, JsonSerializerOptions options) =>
			writer.WriteStringValue(value.Value);
	}

	private sealed class ItemSourceJsonConverter : JsonConverter<ItemSource>
	{
		public override ItemSource Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
			ItemSource.From(reader.GetString()!);

		public override void Write(Utf8JsonWriter writer, ItemSource value, JsonSerializerOptions options) =>
			writer.WriteStringValue(value.Value);
	}
}
