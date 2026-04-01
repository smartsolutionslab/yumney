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

    public async Task DisposeAsync()
    {
        await using var context = await fixture.CreateUsersDbContextAsync();
        var profiles = await context.AppUserProfiles
            .Where(p => p.KeycloakUserId == keycloakId)
            .ToListAsync();
        context.AppUserProfiles.RemoveRange(profiles);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task AddAsync_NewProfile_PersistsToDatabase()
    {
        var profile = CreateProfile();

        await using (var writeContext = await fixture.CreateUsersDbContextAsync())
        {
            var repository = new AppUserProfileRepository(writeContext);
            await repository.AddAsync(profile);
        }

        await using var readContext = await fixture.CreateUsersDbContextAsync();
        var saved = await readContext.AppUserProfiles
            .FirstOrDefaultAsync(p => p.Id == profile.Id);

        saved.Should().NotBeNull();
        saved!.KeycloakUserId.Value.Should().Be(keycloakId.Value);
    }

    [Fact]
    public async Task AddAsync_NewProfile_PersistsDefaultPreferences()
    {
        var profile = CreateProfile();

        await using (var writeContext = await fixture.CreateUsersDbContextAsync())
        {
            var repository = new AppUserProfileRepository(writeContext);
            await repository.AddAsync(profile);
        }

        await using var readContext = await fixture.CreateUsersDbContextAsync();
        var saved = await readContext.AppUserProfiles
            .FirstOrDefaultAsync(p => p.Id == profile.Id);

        saved!.PreferredLanguage.Value.Should().Be("en");
        saved.PreferredUnitSystem.Value.Should().Be("metric");
    }

    [Fact]
    public async Task FindByKeycloakUserIdAsync_ExistingProfile_ReturnsProfile()
    {
        var profile = CreateProfile();

        await using (var writeContext = await fixture.CreateUsersDbContextAsync())
        {
            var repository = new AppUserProfileRepository(writeContext);
            await repository.AddAsync(profile);
        }

        await using var readContext = await fixture.CreateUsersDbContextAsync();
        var repository2 = new AppUserProfileRepository(readContext);
        var loaded = await repository2.FindByKeycloakUserIdAsync(keycloakId);

        loaded.Should().NotBeNull();
        loaded!.DisplayName.Value.Should().Be("Test User");
    }

    [Fact]
    public async Task FindByKeycloakUserIdAsync_NonExistent_ReturnsNull()
    {
        await using var context = await fixture.CreateUsersDbContextAsync();
        var repository = new AppUserProfileRepository(context);

        var loaded = await repository.FindByKeycloakUserIdAsync(KeycloakUserId.From("nonexistent-kc-id"));

        loaded.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_DuplicateKeycloakUserId_ThrowsException()
    {
        var profile1 = CreateProfile("User One");
        var profile2 = CreateProfile("User Two");

        await using (var writeContext = await fixture.CreateUsersDbContextAsync())
        {
            var repository = new AppUserProfileRepository(writeContext);
            await repository.AddAsync(profile1);
        }

        await using var writeContext2 = await fixture.CreateUsersDbContextAsync();
        var repository2 = new AppUserProfileRepository(writeContext2);

        var act = async () => await repository2.AddAsync(profile2);

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task AddAsync_PreservesDisplayName()
    {
        var profile = CreateProfile("Jane Doe");

        await using (var writeContext = await fixture.CreateUsersDbContextAsync())
        {
            var repository = new AppUserProfileRepository(writeContext);
            await repository.AddAsync(profile);
        }

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
