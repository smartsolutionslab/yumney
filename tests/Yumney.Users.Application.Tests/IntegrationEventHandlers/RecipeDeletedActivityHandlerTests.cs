using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Shared.Events.Contracts;
using SmartSolutionsLab.Yumney.Users.Application.IntegrationEventHandlers;
using SmartSolutionsLab.Yumney.Users.Domain.UserActivity;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Application.Tests.IntegrationEventHandlers;

public class RecipeDeletedActivityHandlerTests
{
	private readonly IUserActivityRepository activities = Substitute.For<IUserActivityRepository>();
	private readonly RecipeDeletedActivityHandler handler;

	public RecipeDeletedActivityHandlerTests()
	{
		handler = new RecipeDeletedActivityHandler(activities);
	}

	[Fact]
	public async Task HandleAsync_PersistsRecipeDeletedActivityWithoutTitle()
	{
		var recipeId = Guid.NewGuid();
		var @event = new RecipeDeletedIntegrationEvent("user-123", recipeId);

		UserActivity? captured = null;
		await activities.AddAsync(
			Arg.Do<UserActivity>(activity => captured = activity),
			Arg.Any<CancellationToken>());

		await handler.HandleAsync(@event);

		captured.Should().NotBeNull();
		captured!.Type.Should().Be(ActivityType.RecipeDeleted);
		captured.Owner.Value.Should().Be("user-123");
		captured.RecipeIdentifier!.Value.Should().Be(recipeId);

		// RecipeDeletedIntegrationEvent doesn't carry the title (it's gone by
		// the time the event fires) — handler intentionally records null.
		captured.RecipeTitle.Should().BeNull();
	}

	[Fact]
	public async Task HandleAsync_PassesCancellationTokenThrough()
	{
		var @event = new RecipeDeletedIntegrationEvent("user-123", Guid.NewGuid());
		using var cts = new CancellationTokenSource();

		await handler.HandleAsync(@event, cts.Token);

		await activities.Received(1).AddAsync(Arg.Any<UserActivity>(), cts.Token);
	}
}
