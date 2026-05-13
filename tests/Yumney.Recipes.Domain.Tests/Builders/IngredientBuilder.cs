using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Builders;

/// <summary>
/// Test builder for the <see cref="Ingredient"/> entity. Defaults to the
/// IngredientName builder default ("Flour") and no quantity. Use the
/// <c>WithQuantity</c> overloads to set amount + unit explicitly.
/// </summary>
public sealed class IngredientBuilder
{
	private IngredientName name = IngredientName.From("Flour");
	private Quantity? quantity;

	public static IngredientBuilder A() => new();

	public IngredientBuilder Named(string value)
	{
		name = IngredientName.From(value);
		return this;
	}

	public IngredientBuilder Named(IngredientName value)
	{
		name = value;
		return this;
	}

	public IngredientBuilder WithQuantity(decimal amount, Unit? unit = null)
	{
		quantity = Quantity.Of(Amount.From(amount), unit);
		return this;
	}

	public IngredientBuilder WithQuantity(Quantity value)
	{
		quantity = value;
		return this;
	}

	public IngredientBuilder WithoutQuantity()
	{
		quantity = null;
		return this;
	}

	public Ingredient Build() => Ingredient.Create(name, quantity);

	public static implicit operator Ingredient(IngredientBuilder builder) => builder.Build();
}
