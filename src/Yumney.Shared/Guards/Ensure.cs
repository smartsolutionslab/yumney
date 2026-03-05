using System.Runtime.CompilerServices;

namespace Yumney.Shared.Guards;

public static class Ensure
{
    public static GuardClause<T> That<T>(
        T value,
        [CallerArgumentExpression(nameof(value))] string paramName = "")
    {
        return new GuardClause<T>(value, paramName);
    }
}
