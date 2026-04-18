namespace SmartSolutionsLab.Yumney.Shared.Guards;

public sealed class GuardException(string parameterName, string message) : Exception(message)
{
	public string ParameterName { get; } = parameterName;
}
