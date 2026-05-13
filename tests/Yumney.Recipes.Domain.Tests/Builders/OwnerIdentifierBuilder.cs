using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Builders;

public sealed class OwnerIdentifierBuilder
{
	private string value = "user-123";

	public static OwnerIdentifierBuilder A() => new();

	public OwnerIdentifierBuilder With(string value)
	{
		this.value = value;
		return this;
	}

	public OwnerIdentifier Build() => OwnerIdentifier.From(value);

	public static implicit operator OwnerIdentifier(OwnerIdentifierBuilder builder) => builder.Build();
}
