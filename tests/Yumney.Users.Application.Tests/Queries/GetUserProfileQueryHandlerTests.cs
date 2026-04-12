using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Users.Application.Queries;
using SmartSolutionsLab.Yumney.Users.Application.Queries.Handlers;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Application.Tests.Queries;

public class GetUserProfileQueryHandlerTests
{
    private readonly IAppUserProfileRepository profiles = Substitute.For<IAppUserProfileRepository>();
    private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
    private readonly GetUserProfileQueryHandler handler;

    public GetUserProfileQueryHandlerTests()
    {
        currentUser.UserId.Returns("kc-user-123");
        handler = new GetUserProfileQueryHandler(profiles, currentUser);
    }

    [Fact]
    public async Task HandleAsync_ProfileExists_ReturnsSuccess()
    {
        var profile = AppUserProfile.Create(
            KeycloakUserId.From("kc-user-123"),
            DisplayName.From("Test User"));

        profiles.FindByKeycloakUserIdAsync(Arg.Any<KeycloakUserId>(), Arg.Any<CancellationToken>())
            .Returns(profile);

        var result = await handler.HandleAsync(new GetUserProfileQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.DisplayName.Should().Be("Test User");
        result.Value.DefaultServings.Should().Be(4);
    }

    [Fact]
    public async Task HandleAsync_ProfileNotFound_ReturnsFailure()
    {
        profiles.FindByKeycloakUserIdAsync(Arg.Any<KeycloakUserId>(), Arg.Any<CancellationToken>())
            .Returns((AppUserProfile?)null);

        var result = await handler.HandleAsync(new GetUserProfileQuery());

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("PROFILE_NOT_FOUND");
    }

    [Fact]
    public async Task HandleAsync_MapsDietaryProfile()
    {
        var profile = AppUserProfile.Create(
            KeycloakUserId.From("kc-user-123"),
            DisplayName.From("Test User"));

        profile.UpdateDietaryProfile(DietaryProfile.From(
            DietaryType.From("vegan"),
            [DietaryRestriction.From("gluten-free")],
            WeeklyBalanceGoals.From(4, 0),
            CookingEffortPreference.From("balanced")));

        profiles.FindByKeycloakUserIdAsync(Arg.Any<KeycloakUserId>(), Arg.Any<CancellationToken>())
            .Returns(profile);

        var result = await handler.HandleAsync(new GetUserProfileQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.DietaryProfile.DietaryType.Should().Be("vegan");
        result.Value.DietaryProfile.Restrictions.Should().ContainSingle().Which.Should().Be("gluten-free");
        result.Value.DietaryProfile.MinVeggieMeals.Should().Be(4);
        result.Value.DietaryProfile.MaxRedMeatMeals.Should().Be(0);
        result.Value.DietaryProfile.CookingEffort.Should().Be("balanced");
    }
}
