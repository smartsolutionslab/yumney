using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.Users.Domain.UserActivity;
using SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Users;

[Collection(AspireCollection.Name)]
public class UserActivityCursorPaginationTests(AspireFixture fixture) : IAsyncLifetime
{
	private static readonly PropertyInfo OccurredAtProperty =
		typeof(UserActivity).GetProperty(nameof(UserActivity.OccurredAt))!;

	private readonly OwnerIdentifier owner = OwnerIdentifier.From($"kc-{Guid.NewGuid():N}");

	public Task InitializeAsync() => Task.CompletedTask;

	public Task DisposeAsync() => AspireFixture.CleanupAsync(
		fixture.CreateUsersDbContextAsync,
		ctx => ctx.Set<UserActivity>().Where(activity => activity.Owner == owner));

	[Fact]
	public async Task GetRecentByCursorAsync_NullCursor_ReturnsLatestPageNewestFirst()
	{
		await SeedActivitiesAsync(count: 5);

		await using var context = await fixture.CreateUsersDbContextAsync();
		var repository = new UserActivityRepository(context);

		var page = await repository.GetRecentByCursorAsync(owner, ActivityLimit.From(3), cursor: null);

		page.Should().HaveCount(3);
		page.Select(activity => activity.OccurredAt)
			.Should().BeInDescendingOrder("newest first");
	}

	[Fact]
	public async Task GetRecentByCursorAsync_WithCursor_ReturnsStrictlyOlderEntries()
	{
		var seeded = await SeedActivitiesAsync(count: 5);

		await using var context = await fixture.CreateUsersDbContextAsync();
		var repository = new UserActivityRepository(context);

		var firstPage = await repository.GetRecentByCursorAsync(owner, ActivityLimit.From(2), cursor: null);
		var cursor = ActivityCursor.From(firstPage[^1].OccurredAt, firstPage[^1].Id);

		var secondPage = await repository.GetRecentByCursorAsync(owner, ActivityLimit.From(2), cursor);

		secondPage.Should().HaveCount(2);
		secondPage.Should().AllSatisfy(activity =>
			activity.OccurredAt.Should().BeBefore(firstPage[^1].OccurredAt));
		secondPage.Select(activity => activity.Id)
			.Should().NotContain(firstPage.Select(activity => activity.Id));

		// And the assembled timeline matches the seeded order (newest first).
		var combined = firstPage.Concat(secondPage).Select(activity => activity.OccurredAt).ToList();
		combined.Should().BeInDescendingOrder();
		combined.Should().BeEquivalentTo(seeded.Select(activity => activity.OccurredAt).Take(4));
	}

	[Fact]
	public async Task GetRecentByTypeAndCursorAsync_FilterSurvivesAcrossPages()
	{
		var importedType = ActivityType.From("recipe_imported");
		var cookedType = ActivityType.From("recipe_cooked");

		// Interleave types so a naive non-typed query would return cookedType rows.
		await SeedActivitiesAsync(count: 1, type: importedType, ageMinutes: 10);
		await SeedActivitiesAsync(count: 1, type: cookedType, ageMinutes: 9);
		await SeedActivitiesAsync(count: 1, type: importedType, ageMinutes: 8);
		await SeedActivitiesAsync(count: 1, type: cookedType, ageMinutes: 7);
		await SeedActivitiesAsync(count: 1, type: importedType, ageMinutes: 6);

		await using var context = await fixture.CreateUsersDbContextAsync();
		var repository = new UserActivityRepository(context);

		var firstPage = await repository.GetRecentByTypeAndCursorAsync(owner, importedType, ActivityLimit.From(2), cursor: null);
		firstPage.Should().HaveCount(2);
		firstPage.Should().AllSatisfy(activity => activity.Type.Should().Be(importedType));

		var cursor = ActivityCursor.From(firstPage[^1].OccurredAt, firstPage[^1].Id);
		var secondPage = await repository.GetRecentByTypeAndCursorAsync(owner, importedType, ActivityLimit.From(2), cursor);

		secondPage.Should().HaveCount(1);
		secondPage[0].Type.Should().Be(importedType);
	}

	private async Task<UserActivity[]> SeedActivitiesAsync(int count, ActivityType? type = null, int ageMinutes = 0)
	{
		var activityType = type ?? ActivityType.From("recipe_imported");
		var seeded = new UserActivity[count];
		var baseTime = DateTime.UtcNow.AddMinutes(-(ageMinutes == 0 ? count : ageMinutes));

		for (var index = 0; index < count; index++)
		{
			var activity = UserActivity.Record(owner, activityType);
			OccurredAtProperty.SetValue(activity, baseTime.AddSeconds(index));
			seeded[count - 1 - index] = activity;
		}

		await using var context = await fixture.CreateUsersDbContextAsync();
		context.Set<UserActivity>().AddRange(seeded);
		await context.SaveChangesAsync();

		return seeded;
	}
}
