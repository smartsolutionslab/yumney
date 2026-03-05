using System.Text.RegularExpressions;

namespace Yumney.Shared.Guards;

public static class GuardExtensions
{
    public static GuardClause<T> IsNotNull<T>(this GuardClause<T> guard)
        where T : class
    {
        if (guard.Value is null)
        {
            throw new GuardException($"{guard.ParamName} must not be null.");
        }

        return guard;
    }

    public static GuardClause<string> IsNotNullOrEmpty(this GuardClause<string> guard)
    {
        if (string.IsNullOrEmpty(guard.Value))
        {
            throw new GuardException($"{guard.ParamName} must not be null or empty.");
        }

        return guard;
    }

    public static GuardClause<string> IsNotNullOrWhiteSpace(this GuardClause<string> guard)
    {
        if (string.IsNullOrWhiteSpace(guard.Value))
        {
            throw new GuardException($"{guard.ParamName} must not be null, empty or whitespace.");
        }

        return guard;
    }

    public static GuardClause<string> HasMaxLength(this GuardClause<string> guard, int maxLength)
    {
        guard.IsNotNullOrEmpty();
        if (guard.Value.Length > maxLength)
        {
            throw new GuardException($"{guard.ParamName} must not exceed {maxLength} characters.");
        }

        return guard;
    }

    public static GuardClause<string> MatchesPattern(
        this GuardClause<string> guard,
        string pattern,
        string? message = null)
    {
        guard.IsNotNullOrEmpty();
        if (!Regex.IsMatch(guard.Value, pattern))
        {
            throw new GuardException(message ?? $"{guard.ParamName} has an invalid format.");
        }

        return guard;
    }

    public static GuardClause<T> IsPositive<T>(this GuardClause<T> guard)
        where T : IComparable<T>
    {
        if (guard.Value.CompareTo(default!) <= 0)
        {
            throw new GuardException($"{guard.ParamName} must be positive.");
        }

        return guard;
    }

    public static GuardClause<T> IsNotNegative<T>(this GuardClause<T> guard)
        where T : IComparable<T>
    {
        if (guard.Value.CompareTo(default!) < 0)
        {
            throw new GuardException($"{guard.ParamName} must not be negative.");
        }

        return guard;
    }

    public static GuardClause<T> IsInRange<T>(this GuardClause<T> guard, T min, T max)
        where T : IComparable<T>
    {
        if (guard.Value.CompareTo(min) < 0 || guard.Value.CompareTo(max) > 0)
        {
            throw new GuardException($"{guard.ParamName} must be between {min} and {max}.");
        }

        return guard;
    }

    public static GuardClause<IEnumerable<T>> IsNotEmpty<T>(this GuardClause<IEnumerable<T>> guard)
    {
        guard.IsNotNull();
        if (!guard.Value.Any())
        {
            throw new GuardException($"{guard.ParamName} must not be empty.");
        }

        return guard;
    }

    public static GuardClause<Guid> IsNotEmpty(this GuardClause<Guid> guard)
    {
        if (guard.Value == Guid.Empty)
        {
            throw new GuardException($"{guard.ParamName} must not be empty.");
        }

        return guard;
    }

    public static GuardClause<string> IsValidUrl(this GuardClause<string> guard)
    {
        guard.IsNotNullOrWhiteSpace();
        if (!Uri.TryCreate(guard.Value, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new GuardException($"{guard.ParamName} must be a valid HTTP(S) URL.");
        }

        return guard;
    }

    public static T AndReturn<T>(this GuardClause<T> guard)
    {
        return guard.Value;
    }
}
