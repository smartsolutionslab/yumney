using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Users.Application.DTOs;
using SmartSolutionsLab.Yumney.Users.Application.Interfaces;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

namespace SmartSolutionsLab.Yumney.Users.Application.Commands.Handlers;

public sealed class RegisterUserCommandHandler(
	IKeycloakAdminService keycloakAdmin,
	IUsersUnitOfWork unitOfWork) : ICommandHandler<RegisterUserCommand, Result<RegisterUserResultDto>>
{
	public async Task<Result<RegisterUserResultDto>> HandleAsync(RegisterUserCommand command, CancellationToken cancellationToken = default)
	{
		var (email, password, displayName) = command;

		var keycloakResult = await keycloakAdmin.CreateUserAsync(email, password, displayName, cancellationToken);

		if (keycloakResult.IsFailure) return keycloakResult.Error!;

		var keycloakUserId = keycloakResult.Value;

		var profile = AppUserProfile.Create(keycloakUserId, displayName);
		await unitOfWork.Profiles.AddAsync(profile, cancellationToken);
		await unitOfWork.SaveChangesAsync(cancellationToken);

		return new RegisterUserResultDto("Registration successful. Please check your email to verify your account.");
	}
}
