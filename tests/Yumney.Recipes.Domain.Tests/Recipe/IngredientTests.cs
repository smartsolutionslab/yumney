using FluentAssertions;
using Xunit;
using Yumney.Recipes.Domain.Recipe;

namespace Yumney.Recipes.Domain.Tests.Recipe;

public class IngredientTests
{
    [Fact]
    public void Create_WithAllFields_SetsProperties()
    {
        var name = new IngredientName("Flour");
        var amount = new Amount(500);
        var unit = new Unit("g");

        var ingredient = Ingredient.Create(name, amount, unit);

        ingredient.Id.Should().NotBeEmpty();
        ingredient.Name.Should().Be(name);
        ingredient.Amount.Should().Be(amount);
        ingredient.Unit.Should().Be(unit);
    }

    [Fact]
    public void Create_WithNullAmount_SetsAmountToNull()
    {
        var name = new IngredientName("Salt");

        var ingredient = Ingredient.Create(name, null, new Unit("pinch"));

        ingredient.Amount.Should().BeNull();
    }

    [Fact]
    public void Create_WithNullUnit_SetsUnitToNull()
    {
        var name = new IngredientName("Eggs");
        var amount = new Amount(3);

        var ingredient = Ingredient.Create(name, amount, null);

        ingredient.Unit.Should().BeNull();
    }

    [Fact]
    public void Create_WithNullAmountAndUnit_SetsToNull()
    {
        var name = new IngredientName("Salt to taste");

        var ingredient = Ingredient.Create(name, null, null);

        ingredient.Amount.Should().BeNull();
        ingredient.Unit.Should().BeNull();
    }

    [Fact]
    public void Create_GeneratesUniqueIds()
    {
        var name = new IngredientName("Flour");

        var ingredient1 = Ingredient.Create(name, null, null);
        var ingredient2 = Ingredient.Create(name, null, null);

        ingredient1.Id.Should().NotBe(ingredient2.Id);
    }
}
