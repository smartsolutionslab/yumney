using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Users.Application.Queries;
using SmartSolutionsLab.Yumney.Users.Application.Queries.Handlers;
using SmartSolutionsLab.Yumney.Users.Domain.UserActivity;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Application.Tests.Queries;

public class GetSuggestionsQueryHandlerTests
{
    private readonly IUserActivityRepository activities = Substitute.For<IUserActivityRepository>();
    private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
    private readonly ILogger<GetSuggestionsQueryHandler> logger = Substitute.For<ILogger<GetSuggestionsQueryHandler>>();
    private readonly GetSuggestionsQueryHandler handler;

    public GetSuggestionsQueryHandlerTests()
    {
        currentUser.UserId.Returns("user-123");
        activities.GetRecentAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        handler = new GetSuggestionsQueryHandler(activities, currentUser, logger);
    }

    [Fact]
    public async Task HandleAsync_ReturnsSuccess()
    {
        var result = await handler.HandleAsync(new GetSuggestionsQuery());

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_ReturnsQuickActions()
    {
        var result = await handler.HandleAsync(new GetSuggestionsQuery());

        result.Value.QuickActions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ReturnsAtMostFourQuickActions()
    {
        var result = await handler.HandleAsync(new GetSuggestionsQuery());

        result.Value.QuickActions.Should().HaveCountLessThanOrEqualTo(4);
    }

    [Fact]
    public async Task HandleAsync_WithRecentImport_IncludesCookNow()
    {
        var recentImport = UserActivity.Record(
            OwnerIdentifier.From("user-123"),
            ActivityType.RecipeImported);
        activities.GetRecentAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([recentImport]);

        var result = await handler.HandleAsync(new GetSuggestionsQuery());

        result.Value.QuickActions.Should().Contain("cook_now");
    }

    [Fact]
    public async Task HandleAsync_WithRecentImport_CookNowIsFirst()
    {
        var recentImport = UserActivity.Record(
            OwnerIdentifier.From("user-123"),
            ActivityType.RecipeImported);
        activities.GetRecentAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([recentImport]);

        var result = await handler.HandleAsync(new GetSuggestionsQuery());

        result.Value.QuickActions[0].Should().Be("cook_now");
    }

    [Fact]
    public async Task HandleAsync_WithRecentImport_IncludesAddToShoppingList()
    {
        var recentImport = UserActivity.Record(
            OwnerIdentifier.From("user-123"),
            ActivityType.RecipeImported);
        activities.GetRecentAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([recentImport]);

        var result = await handler.HandleAsync(new GetSuggestionsQuery());

        result.Value.QuickActions.Should().Contain("add_to_shopping_list");
    }

    [Fact]
    public async Task HandleAsync_WithoutRecentImport_DoesNotIncludeCookNow()
    {
        var oldView = UserActivity.Record(
            OwnerIdentifier.From("user-123"),
            ActivityType.RecipeViewed);
        activities.GetRecentAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([oldView]);

        var result = await handler.HandleAsync(new GetSuggestionsQuery());

        result.Value.QuickActions.Should().NotContain("cook_now");
    }

    [Fact]
    public async Task HandleAsync_QuickActionsHaveNoDuplicates()
    {
        var result = await handler.HandleAsync(new GetSuggestionsQuery());

        result.Value.QuickActions.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task HandleAsync_QueriesRecentActivities()
    {
        await handler.HandleAsync(new GetSuggestionsQuery());

        await activities.Received(1).GetRecentAsync(
            Arg.Any<OwnerIdentifier>(),
            10,
            Arg.Any<CancellationToken>());
    }
}
