using System.Net.Mail;
using System.Numerics;
using System.Text.RegularExpressions;

namespace SmartSolutionsLab.Yumney.Shared.Guards;

#pragma warning disable CA1708 // Identifiers should differ by more than case (C# 14 extension blocks generate same-cased identifiers)
public static class GuardExtensions
{
    extension(GuardClause<string> guard)
    {
        public GuardClause<string> IsNotNullOrWhiteSpace()
        {
            if (string.IsNullOrWhiteSpace(guard.Value))
            {
                throw new GuardException(guard.ParameterName, $"{guard.ParameterName} must not be null or whitespace.");
            }

            return guard;
        }

        public GuardClause<string> HasMaxLength(int maxLength)
        {
            if (guard.Value?.Length > maxLength)
            {
                throw new GuardException(
                    guard.ParameterName,
                    $"{guard.ParameterName} must not exceed {maxLength} characters.");
            }

            return guard;
        }

        public GuardClause<string> HasMinLength(int minLength)
        {
            if (guard.Value is null || guard.Value.Length < minLength)
            {
                throw new GuardException(
                    guard.ParameterName,
                    $"{guard.ParameterName} must be at least {minLength} characters.");
            }

            return guard;
        }

        public GuardClause<string> IsValidEmail()
        {
            if (!MailAddress.TryCreate(guard.Value, out _))
            {
                throw new GuardException(
                    guard.ParameterName,
                    $"{guard.ParameterName} must be a valid email address.");
            }

            return guard;
        }

        public GuardClause<string> Matches(string pattern, string? message = null)
        {
            if (guard.Value is null || !Regex.IsMatch(guard.Value, pattern))
            {
                throw new GuardException(
                    guard.ParameterName,
                    message ?? $"{guard.ParameterName} must match the required pattern.");
            }

            return guard;
        }

        public GuardClause<string> IsValidUrl()
        {
            if (string.IsNullOrWhiteSpace(guard.Value) ||
                !Uri.TryCreate(guard.Value, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new GuardException(guard.ParameterName, $"{guard.ParameterName} must be a valid HTTP(S) URL.");
            }

            return guard;
        }

        public GuardClause<string> IsOneOf(params string[] allowed)
        {
            if (guard.Value is null || !allowed.Contains(guard.Value))
            {
                throw new GuardException(
                    guard.ParameterName,
                    $"{guard.ParameterName} must be one of: {string.Join(", ", allowed)}.");
            }

            return guard;
        }
    }

    extension<T>(GuardClause<T> guard)
        where T : struct, INumber<T>
    {
        public GuardClause<T> IsPositive()
        {
            if (guard.Value <= T.Zero)
            {
                throw new GuardException(guard.ParameterName, $"{guard.ParameterName} must be positive.");
            }

            return guard;
        }

        public GuardClause<T> IsNotNegative()
        {
            if (guard.Value < T.Zero)
            {
                throw new GuardException(guard.ParameterName, $"{guard.ParameterName} must not be negative.");
            }

            return guard;
        }

        public GuardClause<T> IsInRange(T min, T max)
        {
            if (guard.Value < min || guard.Value > max)
            {
                throw new GuardException(
                    guard.ParameterName,
                    $"{guard.ParameterName} must be between {min} and {max}.");
            }

            return guard;
        }
    }

    extension(GuardClause<Guid> guard)
    {
        public GuardClause<Guid> IsNotEmpty()
        {
            if (guard.Value == Guid.Empty)
            {
                throw new GuardException(guard.ParameterName, $"{guard.ParameterName} must not be empty.");
            }

            return guard;
        }
    }

    extension<T>(GuardClause<T> guard)
        where T : class
    {
        public GuardClause<T> IsNotNull()
        {
            if (guard.Value is null) throw new GuardException(guard.ParameterName, $"{guard.ParameterName} must not be null.");
            return guard;
        }
    }

    extension<T>(GuardClause<T> guard)
        where T : struct, Enum
    {
        public GuardClause<T> IsDefinedEnum()
        {
            if (!Enum.IsDefined(guard.Value))
            {
                throw new GuardException(guard.ParameterName, $"{guard.ParameterName} is not a valid {typeof(T).Name}.");
            }

            return guard;
        }
    }

    extension<T>(GuardClause<IReadOnlyCollection<T>> guard)
    {
        public GuardClause<IReadOnlyCollection<T>> IsNotEmpty()
        {
            if (guard.Value is null || guard.Value.Count == 0)
            {
                throw new GuardException(guard.ParameterName, $"{guard.ParameterName} must not be empty.");
            }

            return guard;
        }
    }

    extension<T>(GuardClause<IReadOnlyList<T>> guard)
    {
        public GuardClause<IReadOnlyList<T>> IsNotEmpty()
        {
            if (guard.Value is null || guard.Value.Count == 0)
            {
                throw new GuardException(guard.ParameterName, $"{guard.ParameterName} must not be empty.");
            }

            return guard;
        }
    }

    extension<T>(GuardClause<IEnumerable<T>> guard)
    {
        public GuardClause<IEnumerable<T>> IsNotEmpty()
        {
            if (guard.Value is null || !guard.Value.Any())
            {
                throw new GuardException(guard.ParameterName, $"{guard.ParameterName} must not be empty.");
            }

            return guard;
        }
    }
}
