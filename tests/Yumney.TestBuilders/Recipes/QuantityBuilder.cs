using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.TestBuilders.Recipes;

/// <summary>
/// Composite VO builder. <see cref="Quantity.Of"/> takes an <see cref="Amount"/>
/// and a nullable <see cref="Unit"/>; this builder exposes one <c>With*</c> per
/// component so tests can vary either independently.
/// </summary>
public sealed class QuantityBuilder
{
	private Amount amount = Amount.From(1m);
	private Unit? unit = Unit.Gram;

	public static QuantityBuilder A() => new();

	public QuantityBuilder WithAmount(decimal value)
	{
		amount = Amount.From(value);
		return this;
	}

	public QuantityBuilder WithAmount(Amount value)
	{
		amount = value;
		return this;
	}

	public QuantityBuilder WithUnit(Unit? value)
	{
		unit = value;
		return this;
	}

	public QuantityBuilder WithoutUnit()
	{
		unit = null;
		return this;
	}

	public Quantity Build() => Quantity.Of(amount, unit);

	public static implicit operator Quantity(QuantityBuilder builder) => builder.Build();
}
