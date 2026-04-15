using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
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
        },
    };

    [Fact]
    public void ShoppingItemAdded_RoundTrip_PreservesValues()
    {
        var original = new ShoppingItemAdded(ItemName.From("Milk"), Amount.From(2.5m), Unit.From("L"), "manual");

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<ShoppingItemAdded>(json, JsonOptions)!;

        deserialized.ItemName.Value.Should().Be("Milk");
        deserialized.Quantity.Value.Should().Be(2.5m);
        deserialized.Unit!.Value.Should().Be("L");
        deserialized.Source.Should().Be("manual");
    }

    [Fact]
    public void ShoppingItemAdded_SerializesAsFlatJson()
    {
        var @event = new ShoppingItemAdded(ItemName.From("Eggs"), Amount.From(6), Unit.From("pc"), "recipe:abc");

        var json = JsonSerializer.Serialize(@event, JsonOptions);

        json.Should().Contain("\"itemName\":\"Eggs\"");
        json.Should().Contain("\"quantity\":6");
        json.Should().Contain("\"unit\":\"pc\"");
        json.Should().Contain("\"source\":\"recipe:abc\"");
    }

    [Fact]
    public void ShoppingItemAdded_DeserializesFromLegacyJson()
    {
        var legacyJson = """{"itemName":"Butter","quantity":250,"unit":"g","source":"manual"}""";

        var deserialized = JsonSerializer.Deserialize<ShoppingItemAdded>(legacyJson, JsonOptions)!;

        deserialized.ItemName.Value.Should().Be("Butter");
        deserialized.Quantity.Value.Should().Be(250);
        deserialized.Unit!.Value.Should().Be("g");
    }

    [Fact]
    public void ShoppingItemBought_WithNullUnit_RoundTrips()
    {
        var original = new ShoppingItemBought(ItemName.From("Banana"), Amount.From(3), null);

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<ShoppingItemBought>(json, JsonOptions)!;

        deserialized.ItemName.Value.Should().Be("Banana");
        deserialized.Quantity.Value.Should().Be(3);
        deserialized.Unit.Should().BeNull();
    }

    [Fact]
    public void ShoppingItemRemoved_RoundTrip_PreservesReason()
    {
        var original = new ShoppingItemRemoved(ItemName.From("Milk"), Amount.From(1), Unit.From("L"), RemovalReason.From("spoiled"));

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<ShoppingItemRemoved>(json, JsonOptions)!;

        deserialized.Reason!.Value.Should().Be("spoiled");
    }

    [Fact]
    public void ShoppingItemQuantityAdjusted_RoundTrip()
    {
        var original = new ShoppingItemQuantityAdjusted(ItemName.From("Rice"), Amount.From(1.5m), Unit.From("kg"));

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<ShoppingItemQuantityAdjusted>(json, JsonOptions)!;

        deserialized.NewQuantity.Value.Should().Be(1.5m);
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
}
