using System.Runtime.CompilerServices;

namespace SmartSolutionsLab.Yumney.Shared.Guards;

public sealed class GuardClause<T>
{
	private readonly T value;
	private readonly string parameterName;

	internal GuardClause(T value, [CallerArgumentExpression(nameof(value))] string parameterName = "")
	{
		this.value = value;
		this.parameterName = parameterName;
	}

	internal T Value => value;

	internal string ParameterName => parameterName;

	public GuardClause<T> AndReturn() => this;

	public static implicit operator T(GuardClause<T> guard) => guard.value;
}
