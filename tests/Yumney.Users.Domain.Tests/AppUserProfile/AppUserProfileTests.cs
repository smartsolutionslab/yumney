using FluentAssertions;
using Xunit;
using Yumney.Users.Domain.AppUserProfile;

namespace Yumney.Users.Domain.Tests.AppUserProfile;

public class AppUserProfileTests
{
    private static readonly KeycloakUserId TestKeycloakUserId = new("kc-user-123");
    private static readonly DisplayName TestDisplayName = new("Test User");

    [Fact]
    public void Create_ValidParameters_ReturnsProfileWithCorrectValues()
    {
        var profile = Domain.AppUserProfile.AppUserProfile.Create(TestKeycloakUserId, TestDisplayName);

        profile.KeycloakUserId.Should().Be(TestKeycloakUserId);
        profile.DisplayName.Should().Be(TestDisplayName);
    }

    [Fact]
    public void Create_ValidParameters_GeneratesNonEmptyId()
    {
        var profile = Domain.AppUserProfile.AppUserProfile.Create(TestKeycloakUserId, TestDisplayName);

        profile.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_ValidParameters_SetsDefaultPreferredLanguage()
    {
        var profile = Domain.AppUserProfile.AppUserProfile.Create(TestKeycloakUserId, TestDisplayName);

        profile.PreferredLanguage.Should().Be(new PreferredLanguage("en"));
    }

    [Fact]
    public void Create_ValidParameters_SetsDefaultPreferredUnitSystem()
    {
        var profile = Domain.AppUserProfile.AppUserProfile.Create(TestKeycloakUserId, TestDisplayName);

        profile.PreferredUnitSystem.Should().Be(new PreferredUnitSystem("metric"));
    }

    [Fact]
    public void Create_CalledTwice_GeneratesDifferentIds()
    {
        var profile1 = Domain.AppUserProfile.AppUserProfile.Create(TestKeycloakUserId, TestDisplayName);
        var profile2 = Domain.AppUserProfile.AppUserProfile.Create(TestKeycloakUserId, TestDisplayName);

        profile1.Id.Should().NotBe(profile2.Id);
    }

    [Fact]
    public void RenameAs_NewDisplayName_UpdatesDisplayName()
    {
        var profile = Domain.AppUserProfile.AppUserProfile.Create(TestKeycloakUserId, TestDisplayName);
        var newName = new DisplayName("New Name");

        profile.RenameAs(newName);

        profile.DisplayName.Should().Be(newName);
    }

    [Fact]
    public void ChangePreferredLanguage_NewLanguage_UpdatesLanguage()
    {
        var profile = Domain.AppUserProfile.AppUserProfile.Create(TestKeycloakUserId, TestDisplayName);
        var german = new PreferredLanguage("de");

        profile.ChangePreferredLanguage(german);

        profile.PreferredLanguage.Should().Be(german);
    }

    [Fact]
    public void ChangePreferredUnitSystem_NewUnitSystem_UpdatesUnitSystem()
    {
        var profile = Domain.AppUserProfile.AppUserProfile.Create(TestKeycloakUserId, TestDisplayName);
        var imperial = new PreferredUnitSystem("imperial");

        profile.ChangePreferredUnitSystem(imperial);

        profile.PreferredUnitSystem.Should().Be(imperial);
    }
}
