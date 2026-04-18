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
			RecipeIdentifierSnapshot.From(Guid.NewGuid()),
			RecipeTitleSnapshot.From("Test Recipe"));

		activities.GetRecentAsync(Arg.Any<OwnerIdentifier>(), 5, Arg.Any<CancellationToken>())
			.Returns(new List<UserActivity> { activity });

		var result = await handler.HandleAsync(new GetRecentActivityQuery());

		result.IsSuccess.Should().BeTrue();
		result.Value.Should().HaveCount(1);
		result.Value[0].Type.Should().Be("recipe_imported");
	}

	[Fact]
	public async Task HandleAsync_EmptyList_ReturnsEmpty()
	{
		activities.GetRecentAsync(Arg.Any<OwnerIdentifier>(), 5, Arg.Any<CancellationToken>())
			.Returns(new List<UserActivity>());

		var result = await handler.HandleAsync(new GetRecentActivityQuery());

		result.IsSuccess.Should().BeTrue();
		result.Value.Should().BeEmpty();
	}

	[Fact]
	public async Task HandleAsync_RespectsLimit()
	{
		var query = new GetRecentActivityQuery(Limit: 10);

		await handler.HandleAsync(query);

		await activities.Received(1).GetRecentAsync(Arg.Any<OwnerIdentifier>(), 10, Arg.Any<CancellationToken>());
	}
}
