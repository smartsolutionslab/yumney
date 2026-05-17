using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Events.Contracts;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Events.Tests.Contracts;

/// <summary>
/// Construction + base-property smoke tests for every cross-module integration
/// event contract. The records inherit EventIdentifier + OccurredOn from
/// <see cref="IntegrationEvent"/>; we assert each ctor stamps every payload
/// field through and that the base infrastructure auto-populates correctly.
/// </summary>
public class IntegrationEventContractsTests
{
	[Fact]
	public void UserAccountDeletedIntegrationEvent_StampsKeycloakUserId()
	{
		var @event = new UserAccountDeletedIntegrationEvent("kc-user-1");

		@event.KeycloakUserId.Should().Be("kc-user-1");
		@event.EventIdentifier.Should().NotBe(Guid.Empty);
		@event.OccurredOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
	}

	[Fact]
	public void RecipeImportedIntegrationEvent_StampsAllFields()
	{
		var recipe = Guid.NewGuid();

		var @event = new RecipeImportedIntegrationEvent("user-1", recipe, "Carbonara");

		@event.OwnerId.Should().Be("user-1");
		@event.RecipeIdentifier.Should().Be(recipe);
		@event.RecipeTitle.Should().Be("Carbonara");
	}

	[Fact]
	public void RecipeDeletedIntegrationEvent_StampsAllFields()
	{
		var recipe = Guid.NewGuid();

		var @event = new RecipeDeletedIntegrationEvent("user-1", recipe);

		@event.OwnerId.Should().Be("user-1");
		@event.RecipeIdentifier.Should().Be(recipe);
	}

	[Fact]
	public void RecipeViewedIntegrationEvent_StampsAllFields()
	{
		var recipe = Guid.NewGuid();

		var @event = new RecipeViewedIntegrationEvent("user-1", recipe, "Soup");

		@event.OwnerId.Should().Be("user-1");
		@event.RecipeIdentifier.Should().Be(recipe);
		@event.RecipeTitle.Should().Be("Soup");
	}

	[Fact]
	public void RecipeCookedIntegrationEvent_StampsAllFields()
	{
		var recipe = Guid.NewGuid();

		var @event = new RecipeCookedIntegrationEvent("user-1", recipe, "Risotto");

		@event.OwnerId.Should().Be("user-1");
		@event.RecipeIdentifier.Should().Be(recipe);
		@event.RecipeTitle.Should().Be("Risotto");
	}

	[Fact]
	public void MealConfirmedIntegrationEvent_StampsAllFields()
	{
		var recipe = Guid.NewGuid();
		IReadOnlyList<MealConfirmedIngredient> ingredients =
		[
			new("Onion", 1m, null),
			new("Stock", 500m, "ml"),
		];

		var @event = new MealConfirmedIntegrationEvent("user-1", recipe, Servings: 4, ingredients);

		@event.OwnerId.Should().Be("user-1");
		@event.RecipeIdentifier.Should().Be(recipe);
		@event.Servings.Should().Be(4);
		@event.Ingredients.Should().HaveCount(2);
	}

	[Fact]
	public void MealConfirmedIngredient_NullUnit_IsAllowed()
	{
		var ingredient = new MealConfirmedIngredient("Egg", 3m, Unit: null);

		ingredient.Name.Should().Be("Egg");
		ingredient.Quantity.Should().Be(3m);
		ingredient.Unit.Should().BeNull();
	}

	[Fact]
	public void IntegrationEvent_OverridingInitProperties_Roundtrips()
	{
		// Wolverine rehydrates events through System.Text.Json — the `init`
		// setters on EventIdentifier + OccurredOn must accept incoming values
		// or the inbox-dedup key would be regenerated on every redelivery.
		var fixedId = Guid.NewGuid();
		var fixedTime = new DateTime(2026, 5, 17, 10, 0, 0, DateTimeKind.Utc);

		var @event = new RecipeDeletedIntegrationEvent("user-1", Guid.NewGuid())
		{
			EventIdentifier = fixedId,
			OccurredOn = fixedTime,
		};

		@event.EventIdentifier.Should().Be(fixedId);
		@event.OccurredOn.Should().Be(fixedTime);
	}
}
