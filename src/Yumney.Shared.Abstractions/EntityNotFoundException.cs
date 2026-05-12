namespace SmartSolutionsLab.Yumney.Shared.Abstractions;

public sealed class EntityNotFoundException(string entityName, object identifier)
	: Exception($"{entityName} with identifier '{identifier}' was not found.")
{
	public string EntityName { get; } = entityName;

	public object Identifier { get; } = identifier;
}
