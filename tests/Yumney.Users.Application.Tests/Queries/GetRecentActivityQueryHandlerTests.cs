using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Users.Application.Queries;
using SmartSolutionsLab.Yumney.Users.Application.Queries.Handlers;
using SmartSolutionsLab.Yumney.Users.Domain.UserActivity;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Application.Tests.Queries;

public class GetRecentActivityQueryHandlerTests
{
	private readonly IUserActivityRepository activities = Substitute.For<IUserActivityRepository>();
	private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
	private readonly GetRecentActivityQueryHandler handler;

	public GetRecentActivityQueryHandlerTests()
	{
		currentUser.UserId.Returns("user-123");
		handler = new GetRecentActivityQueryHandler(activities, currentUser);
	}

	[Fact]
	public async Task HandleAsync_ReturnsActivities()
	{
		var owner = OwnerIdentifier.From("user-123");
		var activity = UserActivity.Record(
			owner,
			ActivityType.From("recipe_imported"),
			RecipeIdentifierSnapshot.New(),
			RecipeTitleSnapshot.From("Test Recipe"));

		activities.GetRecentByCursorAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<ActivityLimit>(), Arg.Any<ActivityCursor?>(), Arg.Any<CancellationToken>())
			.Returns(new List<UserActivity> { activity });

		var result = await handler.HandleAsync(new GetRecentActivityQuery(ActivityLimit.Default()));

		result.IsSuccess.Should().BeTrue();
		result.Value.Items.Should().HaveCount(1);
		result.Value.Items[0].Type.Should().Be("recipe_imported");
	}

	[Fact]
	public async Task HandleAsync_EmptyList_ReturnsEmptyPageWithoutCursor()
	{
		activities.GetRecentByCursorAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<ActivityLimit>(), Arg.Any<ActivityCursor?>(), Arg.Any<CancellationToken>())
			.Returns(new List<UserActivity>());

		var result = await handler.HandleAsync(new GetRecentActivityQuery(ActivityLimit.Default()));

		result.IsSuccess.Should().BeTrue();
		result.Value.Items.Should().BeEmpty();
		result.Value.NextCursor.Should().BeNull();
	}

	[Fact]
	public async Task HandleAsync_RespectsLimit()
	{
		var limit = ActivityLimit.From(10);
		activities.GetRecentByCursorAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<ActivityLimit>(), Arg.Any<ActivityCursor?>(), Arg.Any<CancellationToken>())
			.Returns(new List<UserActivity>());

		await handler.HandleAsync(new GetRecentActivityQuery(limit));

		await activities.Received(1).GetRecentByCursorAsync(Arg.Any<OwnerIdentifier>(), limit, Arg.Any<ActivityCursor?>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_PageSizeMatchesLimit_EmitsNextCursor()
	{
		var owner = OwnerIdentifier.From("user-123");
		var rows = Enumerable.Range(0, 5)
			.Select(_ => UserActivity.Record(owner, ActivityType.From("recipe_imported")))
			.ToList();
		activities.GetRecentByCursorAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<ActivityLimit>(), Arg.Any<ActivityCursor?>(), Arg.Any<CancellationToken>())
			.Returns(rows);

		var result = await handler.HandleAsync(new GetRecentActivityQuery(ActivityLimit.From(5)));

		result.Value.NextCursor.Should().NotBeNull();
		ActivityCursor.TryDecode(result.Value.NextCursor).Should().NotBeNull();
	}

	[Fact]
	public async Task HandleAsync_PageSmallerThanLimit_NoNextCursor()
	{
		var owner = OwnerIdentifier.From("user-123");
		var rows = new List<UserActivity>
		{
			UserActivity.Record(owner, ActivityType.From("recipe_imported")),
		};
		activities.GetRecentByCursorAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<ActivityLimit>(), Arg.Any<ActivityCursor?>(), Arg.Any<CancellationToken>())
			.Returns(rows);

		var result = await handler.HandleAsync(new GetRecentActivityQuery(ActivityLimit.From(10)));

		result.Value.NextCursor.Should().BeNull();
	}

	[Fact]
	public async Task HandleAsync_TypeFilter_RoutesToTypedRepository()
	{
		activities.GetRecentByTypeAndCursorAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<ActivityType>(), Arg.Any<ActivityLimit>(), Arg.Any<ActivityCursor?>(), Arg.Any<CancellationToken>())
			.Returns(new List<UserActivity>());

		await handler.HandleAsync(new GetRecentActivityQuery(ActivityLimit.Default(), ActivityType.From("recipe_cooked")));

		await activities.Received(1).GetRecentByTypeAndCursorAsync(
			Arg.Any<OwnerIdentifier>(),
			Arg.Is<ActivityType>(type => type.Value == "recipe_cooked"),
			Arg.Any<ActivityLimit>(),
			Arg.Any<ActivityCursor?>(),
			Arg.Any<CancellationToken>());
		await activities.DidNotReceive().GetRecentByCursorAsync(default!, default!, default, default);
	}

	[Fact]
	public async Task HandleAsync_ForwardsCursorToRepository()
	{
		var cursor = ActivityCursor.From(DateTime.UtcNow.AddDays(-1), UserActivityIdentifier.New());
		activities.GetRecentByCursorAsync(Arg.Any<OwnerIdentifier>(), Arg.Any<ActivityLimit>(), Arg.Any<ActivityCursor?>(), Arg.Any<CancellationToken>())
			.Returns(new List<UserActivity>());

		await handler.HandleAsync(new GetRecentActivityQuery(ActivityLimit.Default(), Cursor: cursor));

		await activities.Received(1).GetRecentByCursorAsync(
			Arg.Any<OwnerIdentifier>(),
			Arg.Any<ActivityLimit>(),
			cursor,
			Arg.Any<CancellationToken>());
	}
}
