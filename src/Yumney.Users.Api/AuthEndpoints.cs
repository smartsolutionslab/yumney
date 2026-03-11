using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Users.Application.Commands;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

namespace SmartSolutionsLab.Yumney.Users.Api;

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
        RegisterUserRequest request,
        IValidator<RegisterUserRequest> validator,
        ICommandHandler<RegisterUserCommand, Result<RegisterUserResultDto>> handler,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var command = new RegisterUserCommand(
            new Email(request.Email),
            new Password(request.Password),
            new DisplayName(request.DisplayName));

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
        ResendVerificationEmailRequest request,
        IValidator<ResendVerificationEmailRequest> validator,
        ICommandHandler<ResendVerificationEmailCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var command = new ResendVerificationEmailCommand(new Email(request.Email));

        var result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error switch
            {
                VerificationErrors.IdentityProviderUnavailable => Results.Problem("Identity provider is unavailable.", statusCode: 503),
                _ => Results.Ok(new { message = "If this email is registered, a verification email has been sent." }),
            };
        }

        return Results.Ok(new { message = "If this email is registered, a verification email has been sent." });
    }
}
