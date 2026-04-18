using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Users.Application.Interfaces;

namespace SmartSolutionsLab.Yumney.Users.Application.Commands.Handlers;

public sealed class ResendVerificationEmailCommandHandler(IKeycloakAdminService keycloakAdmin)
	: ICommandHandler<ResendVerificationEmailCommand, Result>
{
	public async Task<Result> HandleAsync(ResendVerificationEmailCommand command, CancellationToken cancellationToken = default)
	{
		var email = command.Email;

		var findResult = await keycloakAdmin.FindUserByEmailAsync(email, cancellationToken);

		if (findResult.IsFailure)
		{
			if (findResult.Error == VerificationErrors.UserNotFound)
			{
				return Result.Success();
			}

			return Result.Failure(findResult.Error!);
		}

		var keycloakUserId = findResult.Value;

		var sendResult = await keycloakAdmin.SendVerificationEmailAsync(keycloakUserId, cancellationToken);

		if (sendResult.IsFailure) return Result.Failure(sendResult.Error!);

		return Result.Success();
	}
}
