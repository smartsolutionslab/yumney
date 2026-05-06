using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shared.Web;
using SmartSolutionsLab.Yumney.Users.Application.Commands;
using SmartSolutionsLab.Yumney.Users.Application.DTOs;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

namespace SmartSolutionsLab.Yumney.Users.Api;

public static class AuthEndpoints
{
	public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
	{
		var group = app.MapGroup("/auth");

		group.MapPost("/register", Register)
			.AllowAnonymous()
			.RequireRateLimiting(RateLimitPolicies.AnonymousAuth)
			.WithName("RegisterUser")
			.WithTags("Auth")
			.WithValidation<Requests.RegisterUser>()
			.Produces<RegisterUserResultDto>(StatusCodes.Status201Created)
			.ProducesValidationProblem()
			.ProducesProblem(StatusCodes.Status409Conflict)
			.ProducesProblem(StatusCodes.Status429TooManyRequests);

		static async Task<IResult> Register(
			Requests.RegisterUser request,
			ICommandHandler<RegisterUserCommand, Result<RegisterUserResultDto>> handler,
			CancellationToken cancellationToken)
		{
			var (email, password, displayName) = request.ToValueObjects();
			var command = new RegisterUserCommand(email, password, displayName);
			var result = await handler.HandleAsync(command, cancellationToken);
			return result.ToCreated("/api/v1/users/me");
		}

		group.MapPost("/resend-verification-email", ResendVerificationEmail)
			.AllowAnonymous()
			.RequireRateLimiting(RateLimitPolicies.AnonymousAuth)
			.WithName("ResendVerificationEmail")
			.WithTags("Auth")
			.WithValidation<Requests.ResendVerificationEmail>()
			.Produces<ResendVerificationEmailResultDto>()
			.ProducesValidationProblem()
			.ProducesProblem(StatusCodes.Status429TooManyRequests);

		static async Task<IResult> ResendVerificationEmail(
			Requests.ResendVerificationEmail request,
			ICommandHandler<ResendVerificationEmailCommand, Result> handler,
			ILoggerFactory loggerFactory,
			CancellationToken cancellationToken)
		{
			var command = new ResendVerificationEmailCommand(Email.From(request.Email));

			// The endpoint is intentionally indistinguishable for "user found",
			// "user not found", and most failures so an attacker can't probe
			// which emails are registered. Only an explicit
			// IdentityProviderUnavailable signal (Keycloak unreachable) leaks
			// out as 503 — anything else collapses to a 200 success body, even
			// unhandled exceptions. Without this catch, a flaky downstream
			// (Redis, Keycloak SMTP) bubbled up as 500, which the SPA mapped
			// to "An unexpected error occurred." instead of "Email sent".
			try
			{
				var result = await handler.HandleAsync(command, cancellationToken);

				if (result.IsFailure && result.Error == VerificationErrors.IdentityProviderUnavailable)
				{
					return Results.Problem(result.Error.Message, statusCode: result.Error.HttpStatusCode);
				}
			}
			catch (Exception ex)
			{
				loggerFactory.CreateLogger("ResendVerificationEmail")
					.LogWarning(ex, "Resend verification flow failed unexpectedly; returning generic success");
			}

			return Results.Ok(new ResendVerificationEmailResultDto("If this email is registered, a verification email has been sent."));
		}

		return app;
	}
}
