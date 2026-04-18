using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;
using SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Users;

[Collection(AspireCollection.Name)]
public class UserProfilePersistenceTests(AspireFixture fixture) : IAsyncLifetime
{
	private readonly KeycloakUserId keycloakId = KeycloakUserId.From($"kc-{Guid.NewGuid():N}");

	public Task InitializeAsync() => Task.CompletedTask;

	public Task DisposeAsync() => AspireFixture.CleanupAsync(
		fixture.CreateUsersDbContextAsync,
		ctx => ctx.AppUserProfiles.Where(p => p.KeycloakUserId == keycloakId));

	[Fact]
	public async Task AddAsync_NewProfile_PersistsWithDefaultPreferences()
	{
		var profile = CreateProfile();
		await fixture.SeedUserProfilesAsync(profile);

		await using var readContext = await fixture.CreateUsersDbContextAsync();
		var saved = await readContext.AppUserProfiles
			.FirstOrDefaultAsync(p => p.Id == profile.Id);

		saved.Should().NotBeNull();
		saved!.KeycloakUserId.Value.Should().Be(keycloakId.Value);
		saved.PreferredLanguage.Value.Should().Be("en");
		saved.PreferredUnitSystem.Value.Should().Be("metric");
	}

	[Fact]
	public async Task FindByKeycloakUserIdAsync_ExistingProfile_ReturnsProfile()
	{
		var profile = CreateProfile();
		await fixture.SeedUserProfilesAsync(profile);

		await using var readContext = await fixture.CreateUsersDbContextAsync();
		var users = new AppUserProfileRepository(readContext);
		var loaded = await users.FindByKeycloakUserIdAsync(keycloakId);

		loaded.Should().NotBeNull();
		loaded!.DisplayName.Value.Should().Be("Test User");
	}

	[Fact]
	public async Task FindByKeycloakUserIdAsync_NonExistent_ReturnsNull()
	{
		await using var context = await fixture.CreateUsersDbContextAsync();
		var users = new AppUserProfileRepository(context);

		var loaded = await users.FindByKeycloakUserIdAsync(KeycloakUserId.From("nonexistent-kc-id"));

		loaded.Should().BeNull();
	}

	[Fact]
	public async Task AddAsync_DuplicateKeycloakUserId_ThrowsException()
	{
		var profile1 = CreateProfile("User One");
		await fixture.SeedUserProfilesAsync(profile1);

		await using var writeContext = await fixture.CreateUsersDbContextAsync();
		var users = new AppUserProfileRepository(writeContext);
		var profile2 = CreateProfile("User Two");

		var act = async () => await users.AddAsync(profile2);

		await act.Should().ThrowAsync<DbUpdateException>();
	}

	[Fact]
	public async Task AddAsync_PreservesDisplayName()
	{
		var profile = CreateProfile("Jane Doe");
		await fixture.SeedUserProfilesAsync(profile);

		await using var readContext = await fixture.CreateUsersDbContextAsync();
		var saved = await readContext.AppUserProfiles
			.FirstOrDefaultAsync(p => p.Id == profile.Id);

		saved!.DisplayName.Value.Should().Be("Jane Doe");
	}

	private AppUserProfile CreateProfile(string? displayName = null)
	{
		return AppUserProfile.Create(keycloakId, DisplayName.From(displayName ?? "Test User"));
	}
}
