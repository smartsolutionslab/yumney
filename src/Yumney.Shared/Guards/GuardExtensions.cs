using System.Net.Mail;
using System.Text.RegularExpressions;

namespace Yumney.Shared.Guards;

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
    }

    public static GuardClause<int> IsPositive(this GuardClause<int> guard)
    {
        if (guard.Value <= 0)
        {
            throw new GuardException(guard.ParameterName, $"{guard.ParameterName} must be positive.");
        }

        return guard;
    }

    public static GuardClause<decimal> IsPositive(this GuardClause<decimal> guard)
    {
        if (guard.Value <= 0)
        {
            throw new GuardException(guard.ParameterName, $"{guard.ParameterName} must be positive.");
        }

        return guard;
    }

    public static GuardClause<decimal> IsNotNegative(this GuardClause<decimal> guard)
    {
        if (guard.Value < 0)
        {
            throw new GuardException(guard.ParameterName, $"{guard.ParameterName} must not be negative.");
        }

        return guard;
    }

    public static GuardClause<int> IsNotNegative(this GuardClause<int> guard)
    {
        if (guard.Value < 0)
        {
            throw new GuardException(guard.ParameterName, $"{guard.ParameterName} must not be negative.");
        }

        return guard;
    }

    public static GuardClause<string> IsValidUrl(this GuardClause<string> guard)
    {
        if (string.IsNullOrWhiteSpace(guard.Value) ||
            !Uri.TryCreate(guard.Value, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new GuardException(guard.ParameterName, $"{guard.ParameterName} must be a valid HTTP(S) URL.");
        }

        return guard;
    }

    public static GuardClause<T> IsNotNull<T>(this GuardClause<T> guard)
        where T : class
    {
        if (guard.Value is null)
        {
            throw new GuardException(guard.ParameterName, $"{guard.ParameterName} must not be null.");
        }

        return guard;
    }

    public static GuardClause<IReadOnlyCollection<T>> IsNotEmpty<T>(this GuardClause<IReadOnlyCollection<T>> guard)
    {
        if (guard.Value is null || guard.Value.Count == 0)
        {
            throw new GuardException(guard.ParameterName, $"{guard.ParameterName} must not be empty.");
        }

        return guard;
    }

    public static GuardClause<IEnumerable<T>> IsNotEmpty<T>(this GuardClause<IEnumerable<T>> guard)
    {
        if (guard.Value is null || !guard.Value.Any())
        {
            throw new GuardException(guard.ParameterName, $"{guard.ParameterName} must not be empty.");
        }

        return guard;
    }

    public static GuardClause<T> IsDefinedEnum<T>(this GuardClause<T> guard)
        where T : struct, Enum
    {
        if (!Enum.IsDefined(guard.Value))
        {
            throw new GuardException(guard.ParameterName, $"{guard.ParameterName} is not a valid {typeof(T).Name}.");
        }

        return guard;
    }

    public static GuardClause<Guid> IsNotEmpty(this GuardClause<Guid> guard)
    {
        if (guard.Value == Guid.Empty)
        {
            throw new GuardException(guard.ParameterName, $"{guard.ParameterName} must not be empty.");
        }

        return guard;
    }

    public static GuardClause<int> IsInRange(this GuardClause<int> guard, int min, int max)
    {
        if (guard.Value < min || guard.Value > max)
        {
            throw new GuardException(
                guard.ParameterName,
                $"{guard.ParameterName} must be between {min} and {max}.");
        }

        return guard;
    }

    public static GuardClause<decimal> IsInRange(this GuardClause<decimal> guard, decimal min, decimal max)
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
