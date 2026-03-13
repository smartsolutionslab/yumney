using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Users.Application.Commands;

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

        var command = RegisterUserCommand.From(request.Email, request.Password, request.DisplayName);
        var result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return Results.Problem(result.Error!.Message, statusCode: result.Error.HttpStatusCode);
        }

        return Results.Created("/api/v1/users/me", result.Value);
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

        var command = ResendVerificationEmailCommand.From(request.Email);
        var result = await handler.HandleAsync(command, cancellationToken);

        // Always return 200 to prevent email enumeration
        if (result.IsFailure && result.Error == VerificationErrors.IdentityProviderUnavailable)
        {
            return Results.Problem(result.Error.Message, statusCode: result.Error.HttpStatusCode);
        }

        return Results.Ok(new ResendVerificationEmailResultDto("If this email is registered, a verification email has been sent."));
    }
}
