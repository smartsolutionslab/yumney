using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Users.Application.Commands;
using SmartSolutionsLab.Yumney.Users.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Application.Tests.Commands;

public class UpdateUserProfileCommandHandlerTests
{
	private readonly IAppUserProfileRepository profiles = Substitute.For<IAppUserProfileRepository>();
	private readonly IUsersUnitOfWork unitOfWork = Substitute.For<IUsersUnitOfWork>();
	private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
	private readonly UpdateUserProfileCommandHandler handler;

	public UpdateUserProfileCommandHandlerTests()
	{
		currentUser.UserId.Returns("kc-user-123");
		unitOfWork.Profiles.Returns(profiles);
		handler = new UpdateUserProfileCommandHandler(unitOfWork, currentUser);
	}

	[Fact]
	public async Task HandleAsync_UpdatesDefaultServings()
	{
		var profile = CreateProfile();
		var command = new UpdateUserProfileCommand(6, null, [], null, null, null);

		var result = await handler.HandleAsync(command);

		result.IsSuccess.Should().BeTrue();
		result.Value.DefaultServings.Should().Be(6);
		await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_UpdatesDietaryType()
	{
		var profile = CreateProfile();
		var command = new UpdateUserProfileCommand(4, "vegetarian", [], null, null, null);

		var result = await handler.HandleAsync(command);

		result.IsSuccess.Should().BeTrue();
		result.Value.DietaryProfile.DietaryType.Should().Be("vegetarian");
	}

	[Fact]
	public async Task HandleAsync_UpdatesRestrictions()
	{
		var profile = CreateProfile();
		var command = new UpdateUserProfileCommand(4, null, ["gluten-free", "nut-allergy"], null, null, null);

		var result = await handler.HandleAsync(command);

		result.IsSuccess.Should().BeTrue();
		result.Value.DietaryProfile.Restrictions.Should().BeEquivalentTo(["gluten-free", "nut-allergy"]);
	}

	[Fact]
	public async Task HandleAsync_UpdatesWeeklyBalanceGoals()
	{
		var profile = CreateProfile();
		var command = new UpdateUserProfileCommand(4, null, [], 3, 2, null);

		var result = await handler.HandleAsync(command);

		result.IsSuccess.Should().BeTrue();
		result.Value.DietaryProfile.MinVeggieMeals.Should().Be(3);
		result.Value.DietaryProfile.MaxRedMeatMeals.Should().Be(2);
	}

	[Fact]
	public async Task HandleAsync_UpdatesCookingEffort()
	{
		var profile = CreateProfile();
		var command = new UpdateUserProfileCommand(4, null, [], null, null, "quick-weekdays");

		var result = await handler.HandleAsync(command);

		result.IsSuccess.Should().BeTrue();
		result.Value.DietaryProfile.CookingEffort.Should().Be("quick-weekdays");
	}

	[Fact]
	public async Task HandleAsync_ReturnsMappedDto()
	{
		var profile = CreateProfile();
		var command = new UpdateUserProfileCommand(4, null, [], null, null, null);

		var result = await handler.HandleAsync(command);

		result.IsSuccess.Should().BeTrue();
		result.Value.DisplayName.Should().Be("Test User");
		result.Value.PreferredLanguage.Should().Be("en");
		result.Value.PreferredUnitSystem.Should().Be("metric");
	}

	private AppUserProfile CreateProfile()
	{
		var profile = AppUserProfile.Create(
			KeycloakUserId.From("kc-user-123"),
			DisplayName.From("Test User"));

		profiles.GetByKeycloakUserIdAsync(Arg.Any<KeycloakUserId>(), Arg.Any<CancellationToken>())
			.Returns(profile);

		return profile;
	}
}
