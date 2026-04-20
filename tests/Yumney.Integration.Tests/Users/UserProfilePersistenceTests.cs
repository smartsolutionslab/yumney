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
		var expectedLanguage = PreferredLanguage.From("en");
		var expectedUnitSystem = PreferredUnitSystem.From("metric");
		var profile = CreateProfile();
		await fixture.SeedUserProfilesAsync(profile);

		await using var readContext = await fixture.CreateUsersDbContextAsync();
		var saved = await readContext.AppUserProfiles
			.FirstOrDefaultAsync(p => p.Id == profile.Id);

		saved.Should().NotBeNull();
		saved!.KeycloakUserId.Should().Be(keycloakId);
		saved.PreferredLanguage.Should().Be(expectedLanguage);
		saved.PreferredUnitSystem.Should().Be(expectedUnitSystem);
	}

	[Fact]
	public async Task FindByKeycloakUserIdAsync_ExistingProfile_ReturnsProfile()
	{
		var displayName = DisplayName.From("Test User");
		var profile = CreateProfile(displayName);
		await fixture.SeedUserProfilesAsync(profile);

		await using var readContext = await fixture.CreateUsersDbContextAsync();
		var users = new AppUserProfileRepository(readContext);
		var loaded = await users.FindByKeycloakUserIdAsync(keycloakId);

		loaded.Should().NotBeNull();
		loaded!.DisplayName.Should().Be(displayName);
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
		var profile1 = CreateProfile(DisplayName.From("User One"));
		await fixture.SeedUserProfilesAsync(profile1);

		await using var writeContext = await fixture.CreateUsersDbContextAsync();
		var users = new AppUserProfileRepository(writeContext);
		var profile2 = CreateProfile(DisplayName.From("User Two"));

		var act = async () => await users.AddAsync(profile2);

		await act.Should().ThrowAsync<DbUpdateException>();
	}

	[Fact]
	public async Task AddAsync_PreservesDisplayName()
	{
		var displayName = DisplayName.From("Jane Doe");
		var profile = CreateProfile(displayName);
		await fixture.SeedUserProfilesAsync(profile);

		await using var readContext = await fixture.CreateUsersDbContextAsync();
		var saved = await readContext.AppUserProfiles
			.FirstOrDefaultAsync(p => p.Id == profile.Id);

		saved!.DisplayName.Should().Be(displayName);
	}

	private AppUserProfile CreateProfile(DisplayName? displayName = null)
	{
		return AppUserProfile.Create(keycloakId, displayName ?? DisplayName.From("Test User"));
	}
}
