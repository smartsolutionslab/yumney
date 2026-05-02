using FluentAssertions;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using Xunit;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.Tests.WeeklyPlan;

public class SlotRecipeReferenceTests
{
	[Fact]
	public void From_ValueObjects_CreatesInstance()
	{
		var recipe = SlotRecipeIdentifier.From(Guid.NewGuid());
		var title = SlotRecipeTitle.From("Pad Thai");

		var reference = SlotRecipeReference.From(recipe, title);

		reference.RecipeIdentifier.Should().Be(recipe);
		reference.Title.Should().Be(title);
	}

	[Fact]
	public void From_RawGuidAndString_CreatesInstance()
	{
		var recipeGuid = Guid.NewGuid();

		var reference = SlotRecipeReference.From(recipeGuid, "Carbonara");

		reference.RecipeIdentifier.Value.Should().Be(recipeGuid);
		reference.Title.Value.Should().Be("Carbonara");
	}

	[Fact]
	public void ToString_FormatsTitleAndIdentifier()
	{
		var recipeGuid = Guid.NewGuid();
		var reference = SlotRecipeReference.From(recipeGuid, "Risotto");

		reference.ToString().Should().Contain("Risotto").And.Contain(recipeGuid.ToString());
	}

	[Fact]
	public void Equality_SameValues_AreEqual()
	{
		var recipeGuid = Guid.NewGuid();
		var first = SlotRecipeReference.From(recipeGuid, "Same");
		var second = SlotRecipeReference.From(recipeGuid, "Same");

		first.Should().Be(second);
	}

	[Fact]
	public void Equality_DifferentRecipeIdentifier_AreNotEqual()
	{
		var first = SlotRecipeReference.From(Guid.NewGuid(), "Same");
		var second = SlotRecipeReference.From(Guid.NewGuid(), "Same");

		first.Should().NotBe(second);
	}

	[Fact]
	public void Equality_DifferentTitle_AreNotEqual()
	{
		var recipeGuid = Guid.NewGuid();
		var first = SlotRecipeReference.From(recipeGuid, "First");
		var second = SlotRecipeReference.From(recipeGuid, "Second");

		first.Should().NotBe(second);
	}
}
