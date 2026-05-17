using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Shared.Events.Contracts;
using SmartSolutionsLab.Yumney.Users.Application.IntegrationEventHandlers;
using SmartSolutionsLab.Yumney.Users.Domain.UserActivity;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Application.Tests.IntegrationEventHandlers;

public class RecipeImportedActivityHandlerTests
{
	private readonly IUserActivityRepository activities = Substitute.For<IUserActivityRepository>();
	private readonly RecipeImportedActivityHandler handler;

	public RecipeImportedActivityHandlerTests()
	{
		handler = new RecipeImportedActivityHandler(activities);
	}

	[Fact]
	public async Task HandleAsync_PersistsRecipeImportedActivity()
	{
		var recipeId = Guid.NewGuid();
		var @event = new RecipeImportedIntegrationEvent("user-123", recipeId, "Imported Recipe");

		UserActivity? captured = null;
		await activities.AddAsync(
			Arg.Do<UserActivity>(activity => captured = activity),
			Arg.Any<CancellationToken>());

		await handler.HandleAsync(@event);

		captured.Should().NotBeNull();
		captured!.Type.Should().Be(ActivityType.RecipeImported);
		captured.Owner.Value.Should().Be("user-123");
		captured.RecipeIdentifier!.Value.Should().Be(recipeId);
		captured.RecipeTitle!.Value.Should().Be("Imported Recipe");
	}

	[Fact]
	public async Task HandleAsync_PassesCancellationTokenThrough()
	{
		var @event = new RecipeImportedIntegrationEvent("user-123", Guid.NewGuid(), "Stew");
		using var cts = new CancellationTokenSource();

		await handler.HandleAsync(@event, cts.Token);

		await activities.Received(1).AddAsync(Arg.Any<UserActivity>(), cts.Token);
	}
}
