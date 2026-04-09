using FluentValidation;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Web;
using SmartSolutionsLab.Yumney.Users.Api.Requests;
using SmartSolutionsLab.Yumney.Users.Application.Commands;
using SmartSolutionsLab.Yumney.Users.Application.DTOs;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

namespace SmartSolutionsLab.Yumney.Users.Api;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth");

        group.MapPost("/register", RegisterAsync)
            .AllowAnonymous()
            .WithName("RegisterUser")
            .WithTags("Auth")
            .Produces<RegisterUserResultDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/resend-verification-email", ResendVerificationEmailAsync)
            .AllowAnonymous()
            .WithName("ResendVerificationEmail")
            .WithTags("Auth")
            .Produces<ResendVerificationEmailResultDto>()
            .ProducesValidationProblem();

        return app;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterUserRequest request,
        IValidator<RegisterUserRequest> validator,
        ICommandHandler<RegisterUserCommand, Result<RegisterUserResultDto>> handler,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (validation.HasFailed()) return validation.ToValidationProblem();

        var (email, password, displayName) = request;

        var command = new RegisterUserCommand(
            Email.From(email),
            Password.From(password),
            DisplayName.From(displayName));

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToCreated("/api/v1/users/me");
    }

    private static async Task<IResult> ResendVerificationEmailAsync(
        ResendVerificationEmailRequest request,
        IValidator<ResendVerificationEmailRequest> validator,
        ICommandHandler<ResendVerificationEmailCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (validation.HasFailed()) return validation.ToValidationProblem();

        var command = new ResendVerificationEmailCommand(Email.From(request.Email));

        var result = await handler.HandleAsync(command, cancellationToken);

        // Always return 200 to prevent email enumeration
        if (result.IsFailure && result.Error == VerificationErrors.IdentityProviderUnavailable)
        {
            return Results.Problem(result.Error.Message, statusCode: result.Error.HttpStatusCode);
        }

        return Results.Ok(new ResendVerificationEmailResultDto("If this email is registered, a verification email has been sent."));
    }
}
