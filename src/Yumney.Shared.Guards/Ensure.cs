using System.Runtime.CompilerServices;

namespace SmartSolutionsLab.Yumney.Shared.Guards;

public static class Ensure
{
	public static GuardClause<T> That<T>(T value, [CallerArgumentExpression(nameof(value))] string parameterName = "")
	{
		return new GuardClause<T>(value, parameterName);
	}
}
