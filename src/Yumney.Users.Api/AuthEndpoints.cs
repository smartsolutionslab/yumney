using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Yumney.Shared.Common;
using Yumney.Shared.CQRS;
using Yumney.Users.Application.Commands;

namespace Yumney.Users.Api;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth");

        group.MapPost("/register", RegisterAsync)
            .AllowAnonymous()
            .WithName("RegisterUser");

        group.MapPost("/resend-verification-email", ResendVerificationEmailAsync)
            .AllowAnonymous()
            .WithName("ResendVerificationEmail");

        return app;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterUserCommand command,
        IValidator<RegisterUserCommand> validator,
        ICommandHandler<RegisterUserCommand, Result<RegisterUserResultDto>> handler,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);

        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error switch
            {
                RegistrationErrors.EmailAlreadyExists =>
                    Results.Conflict(new { error = "A user with this email address already exists." }),
                RegistrationErrors.IdentityProviderUnavailable =>
                    Results.Problem("Identity provider is unavailable.", statusCode: 503),
                _ =>
                    Results.Problem("Failed to create user account.", statusCode: 500),
            };
        }

        return Results.Created("/auth/register", result.Value);
    }

    private static async Task<IResult> ResendVerificationEmailAsync(
        ResendVerificationEmailCommand command,
        IValidator<ResendVerificationEmailCommand> validator,
        ICommandHandler<ResendVerificationEmailCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);

        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error switch
            {
                VerificationErrors.IdentityProviderUnavailable =>
                    Results.Problem("Identity provider is unavailable.", statusCode: 503),
                _ =>
                    Results.Ok(new { message = "If this email is registered, a verification email has been sent." }),
            };
        }

        return Results.Ok(new { message = "If this email is registered, a verification email has been sent." });
    }
}
