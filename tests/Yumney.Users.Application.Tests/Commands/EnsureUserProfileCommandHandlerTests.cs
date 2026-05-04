using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Users.Application.Commands;
using SmartSolutionsLab.Yumney.Users.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Application.Tests.Commands;

public class EnsureUserProfileCommandHandlerTests
{
	private readonly IAppUserProfileRepository profiles = Substitute.For<IAppUserProfileRepository>();
	private readonly IUsersUnitOfWork unitOfWork = Substitute.For<IUsersUnitOfWork>();
	private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
	private readonly EnsureUserProfileCommandHandler handler;

	public EnsureUserProfileCommandHandlerTests()
	{
		currentUser.UserId.Returns("kc-user-123");
		currentUser.Email.Returns("test@yumney.dev");
		currentUser.DisplayName.Returns("Test User");
		unitOfWork.Profiles.Returns(profiles);
		handler = new EnsureUserProfileCommandHandler(unitOfWork, currentUser);
	}

	[Fact]
	public async Task HandleAsync_ProfileExists_DoesNothing()
	{
		var existing = AppUserProfile.Create(
			KeycloakUserId.From("kc-user-123"),
			DisplayName.From("Existing"));
		profiles.FindByKeycloakUserIdAsync(Arg.Any<KeycloakUserId>(), Arg.Any<CancellationToken>())
			.Returns(existing);

		var result = await handler.HandleAsync(new EnsureUserProfileCommand());

		result.IsSuccess.Should().BeTrue();
		await profiles.DidNotReceive().AddAsync(Arg.Any<AppUserProfile>(), Arg.Any<CancellationToken>());
		await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_ProfileMissing_CreatesFromDisplayNameClaim()
	{
		profiles.FindByKeycloakUserIdAsync(Arg.Any<KeycloakUserId>(), Arg.Any<CancellationToken>())
			.Returns((AppUserProfile?)null);

		var result = await handler.HandleAsync(new EnsureUserProfileCommand());

		result.IsSuccess.Should().BeTrue();
		await profiles.Received(1).AddAsync(
			Arg.Is<AppUserProfile>(p => p.DisplayName.Value == "Test User"),
			Arg.Any<CancellationToken>());
		await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_ProfileMissingAndBlankDisplayName_FallsBackToEmail()
	{
		currentUser.DisplayName.Returns(string.Empty);
		profiles.FindByKeycloakUserIdAsync(Arg.Any<KeycloakUserId>(), Arg.Any<CancellationToken>())
			.Returns((AppUserProfile?)null);

		var result = await handler.HandleAsync(new EnsureUserProfileCommand());

		result.IsSuccess.Should().BeTrue();
		await profiles.Received(1).AddAsync(
			Arg.Is<AppUserProfile>(p => p.DisplayName.Value == "test@yumney.dev"),
			Arg.Any<CancellationToken>());
	}
}
