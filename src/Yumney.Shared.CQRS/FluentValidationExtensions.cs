using FluentValidation;

namespace SmartSolutionsLab.Yumney.Shared.CQRS;

public static class FluentValidationExtensions
{
	public static IRuleBuilderOptions<T, string> MustBeValidHttpUrl<T>(this IRuleBuilder<T, string> ruleBuilder, int maxLength)
	{
		return ruleBuilder
			.MaximumLength(maxLength)
			.Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri)
				&& (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
			.WithMessage("A valid HTTP or HTTPS URL is required.");
	}
}
