using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Events.Contracts.Tests;

/// <summary>
/// Wolverine envelopes serialize integration events via System.Text.Json, so
/// the contract surface here is the STJ round-trip — not just the C# type.
/// IntegrationEvent.EventIdentifier and OccurredOn use init setters
/// specifically so the deserialized instance preserves them; without that,
/// the inbox-dedup key would regenerate on every redelivery and exactly-once
/// semantics would silently break (per the comment on IntegrationEvent.cs).
/// These tests pin both the field-shape and the property-survival contracts
/// so any accidental change to a record signature or to the base type fails
/// loudly.
/// </summary>
public class IntegrationEventWireFormatTests
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNameCaseInsensitive = true,
	};

	[Fact]
	public void RecipeImported_RoundTripsThroughJson_PreservingAllFields()
	{
		var original = new RecipeImportedIntegrationEvent("user-123", Guid.NewGuid(), "Lasagne");

		var roundTripped = RoundTrip(original);

		roundTripped.Should().BeEquivalentTo(original);
	}

	[Fact]
	public void RecipeViewed_RoundTripsThroughJson_PreservingAllFields()
	{
		var original = new RecipeViewedIntegrationEvent("user-123", Guid.NewGuid(), "Tomato Soup");

		var roundTripped = RoundTrip(original);

		roundTripped.Should().BeEquivalentTo(original);
	}

	[Fact]
	public void RecipeCooked_RoundTripsThroughJson_PreservingAllFields()
	{
		var original = new RecipeCookedIntegrationEvent("user-123", Guid.NewGuid(), "Chocolate Cake");

		var roundTripped = RoundTrip(original);

		roundTripped.Should().BeEquivalentTo(original);
	}

	[Fact]
	public void RecipeDeleted_RoundTripsThroughJson_PreservingAllFields()
	{
		var original = new RecipeDeletedIntegrationEvent("user-123", Guid.NewGuid());

		var roundTripped = RoundTrip(original);

		roundTripped.Should().BeEquivalentTo(original);
	}

	[Fact]
	public void MealConfirmed_RoundTripsThroughJson_IncludingNestedIngredients()
	{
		var original = new MealConfirmedIntegrationEvent(
			"user-123",
			Guid.NewGuid(),
			Servings: 4,
			Ingredients:
			[
				new MealConfirmedIngredient("Pasta", 500m, "g"),
				new MealConfirmedIngredient("Tomatoes", 800m, "g"),
				new MealConfirmedIngredient("Garlic", 3m, null),
			]);

		var roundTripped = RoundTrip(original);

		roundTripped.Should().BeEquivalentTo(original);
	}

	[Fact]
	public void UserAccountDeleted_RoundTripsThroughJson_PreservingKeycloakUserId()
	{
		var original = new UserAccountDeletedIntegrationEvent("keycloak-user-456");

		var roundTripped = RoundTrip(original);

		roundTripped.Should().BeEquivalentTo(original);
	}

	[Fact]
	public void EventIdentifier_SurvivesJsonRoundTrip_NotRegeneratedOnDeserialize()
	{
		// Pins the dedup contract: IntegrationEvent's EventIdentifier uses init,
		// not auto-property — System.Text.Json must round-trip the original Guid,
		// not generate a fresh one on deserialize. If this regresses, Wolverine's
		// inbox dedup key would change on every redelivery and exactly-once
		// processing would silently break.
		var original = new RecipeViewedIntegrationEvent("user-123", Guid.NewGuid(), "Test");

		var roundTripped = RoundTrip(original);

		roundTripped.EventIdentifier.Should().Be(original.EventIdentifier);
	}

	[Fact]
	public void OccurredOn_SurvivesJsonRoundTrip_NotResetToNow()
	{
		// Pins the audit contract: OccurredOn must reflect when the event was
		// raised, not when the envelope happened to be deserialized. Same init
		// mechanism as EventIdentifier.
		var fixedTime = new DateTime(2026, 4, 1, 12, 30, 0, DateTimeKind.Utc);
		var original = new RecipeViewedIntegrationEvent("user-123", Guid.NewGuid(), "Test")
		{
			OccurredOn = fixedTime,
		};

		var roundTripped = RoundTrip(original);

		roundTripped.OccurredOn.Should().Be(fixedTime);
	}

	private static T RoundTrip<T>(T value)
		where T : IIntegrationEvent
	{
		var json = JsonSerializer.Serialize(value, JsonOptions);
		var deserialized = JsonSerializer.Deserialize<T>(json, JsonOptions);
		deserialized.Should().NotBeNull();
		return deserialized!;
	}
}
