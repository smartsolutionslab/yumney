using FluentAssertions;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Recipe;

public class IngredientTests
{
	[Fact]
	public void Create_WithAllFields_SetsProperties()
	{
		var name = IngredientName.From("Flour");
		var amount = Amount.From(500);
		var unit = Unit.Gram;

		var ingredient = Ingredient.Create(name, Quantity.Of(amount, unit));

		ingredient.Id.Should().NotBeNull();
		ingredient.Name.Should().Be(name);
		ingredient.Quantity.Should().NotBeNull();
		ingredient.Quantity!.Amount.Should().Be(amount);
		ingredient.Quantity.Unit.Should().Be(unit);
	}

	[Fact]
	public void Create_WithNullAmount_SetsAmountToNull()
	{
		var name = IngredientName.From("Salt");

		var ingredient = Ingredient.Create(name, null);

		ingredient.Quantity.Should().BeNull();
	}

	[Fact]
	public void Create_WithNullUnit_SetsUnitToNull()
	{
		var name = IngredientName.From("Eggs");
		var amount = Amount.From(3);

		var ingredient = Ingredient.Create(name, Quantity.Of(amount, null));

		ingredient.Quantity.Should().NotBeNull();
		ingredient.Quantity!.Unit.Should().BeNull();
	}

	[Fact]
	public void Create_WithNullAmountAndUnit_SetsToNull()
	{
		var name = IngredientName.From("Salt to taste");

		var ingredient = Ingredient.Create(name, null);

		ingredient.Quantity.Should().BeNull();
	}

	[Fact]
	public void Create_GeneratesUniqueIds()
	{
		var name = IngredientName.From("Flour");

		var ingredient1 = Ingredient.Create(name, null);
		var ingredient2 = Ingredient.Create(name, null);

		ingredient1.Id.Should().NotBe(ingredient2.Id);
	}
}
