using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Shared.Events.CrossModule;
using SmartSolutionsLab.Yumney.Users.Application.IntegrationEventHandlers;
using SmartSolutionsLab.Yumney.Users.Domain.UserActivity;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Application.Tests.IntegrationEventHandlers;

public class RecipeCookedActivityHandlerTests
{
	private readonly IUserActivityRepository activities = Substitute.For<IUserActivityRepository>();
	private readonly RecipeCookedActivityHandler handler;

	public RecipeCookedActivityHandlerTests()
	{
		handler = new RecipeCookedActivityHandler(activities);
	}

	[Fact]
	public async Task HandleAsync_PersistsRecipeCookedActivity()
	{
		var recipeId = Guid.NewGuid();
		var @event = new RecipeCookedIntegrationEvent("user-123", recipeId, "Pasta");

		UserActivity? captured = null;
		await activities.AddAsync(
			Arg.Do<UserActivity>(activity => captured = activity),
			Arg.Any<CancellationToken>());

		await handler.HandleAsync(@event);

		captured.Should().NotBeNull();
		captured!.Type.Should().Be(ActivityType.RecipeCooked);
		captured.Owner.Value.Should().Be("user-123");
		captured.RecipeIdentifier!.Value.Should().Be(recipeId);
		captured.RecipeTitle!.Value.Should().Be("Pasta");
	}

	[Fact]
	public async Task HandleAsync_ForwardsCancellationToken()
	{
		var cts = new CancellationTokenSource();
		var @event = new RecipeCookedIntegrationEvent("user-123", Guid.NewGuid(), "Pasta");

		await handler.HandleAsync(@event, cts.Token);

		await activities.Received(1).AddAsync(Arg.Any<UserActivity>(), cts.Token);
	}
}
