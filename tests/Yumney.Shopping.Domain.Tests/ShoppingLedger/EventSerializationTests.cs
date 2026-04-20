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
		var original = new ShoppingItemAdded(
			ItemName.From("Milk"),
			Quantity.Of(Amount.From(2.5m), Unit.From("L")),
			ItemSource.Manual);

		var json = JsonSerializer.Serialize(original, JsonOptions);
		var deserialized = JsonSerializer.Deserialize<ShoppingItemAdded>(json, JsonOptions)!;

		deserialized.ItemName.Should().Be(ItemName.From("Milk"));
		deserialized.Quantity.Amount.Should().Be(Amount.From(2.5m));
		deserialized.Quantity.Unit.Should().Be(Unit.From("L"));
		deserialized.Source.Should().Be(ItemSource.Manual);
	}

	[Fact]
	public void ShoppingItemAdded_SerializesAsNestedQuantity()
	{
		var @event = new ShoppingItemAdded(
			ItemName.From("Eggs"),
			Quantity.Of(Amount.From(6), Unit.From("pc")),
			ItemSource.From("recipe:abc"));

		var json = JsonSerializer.Serialize(@event, JsonOptions);

		json.Should().Contain("\"itemName\":\"Eggs\"");
		json.Should().Contain("\"amount\":6");
		json.Should().Contain("\"unit\":\"pc\"");
		json.Should().Contain("\"source\":\"recipe:abc\"");
	}

	[Fact]
	public void ShoppingItemBought_WithNullUnit_RoundTrips()
	{
		var original = new ShoppingItemBought(
			ItemName.From("Banana"),
			Quantity.Of(Amount.From(3), null));

		var json = JsonSerializer.Serialize(original, JsonOptions);
		var deserialized = JsonSerializer.Deserialize<ShoppingItemBought>(json, JsonOptions)!;

		deserialized.ItemName.Should().Be(ItemName.From("Banana"));
		deserialized.Quantity.Amount.Should().Be(Amount.From(3));
		deserialized.Quantity.Unit.Should().BeNull();
	}

	[Fact]
	public void ShoppingItemRemoved_RoundTrip_PreservesReason()
	{
		var original = new ShoppingItemRemoved(
			ItemName.From("Milk"),
			Quantity.Of(Amount.From(1), Unit.From("L")),
			RemovalReason.From("spoiled"));

		var json = JsonSerializer.Serialize(original, JsonOptions);
		var deserialized = JsonSerializer.Deserialize<ShoppingItemRemoved>(json, JsonOptions)!;

		deserialized.Reason.Should().Be(RemovalReason.From("spoiled"));
	}

	[Fact]
	public void ShoppingItemQuantityAdjusted_RoundTrip()
	{
		var original = new ShoppingItemQuantityAdjusted(
			ItemName.From("Rice"),
			Quantity.Of(Amount.From(1.5m), Unit.From("kg")));

		var json = JsonSerializer.Serialize(original, JsonOptions);
		var deserialized = JsonSerializer.Deserialize<ShoppingItemQuantityAdjusted>(json, JsonOptions)!;

		deserialized.NewQuantity.Amount.Should().Be(Amount.From(1.5m));
		deserialized.NewQuantity.Unit.Should().Be(Unit.From("kg"));
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
