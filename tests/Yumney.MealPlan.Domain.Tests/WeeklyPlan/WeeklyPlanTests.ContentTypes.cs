using FluentAssertions;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.TestBuilders.MealPlan;
using Xunit;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.Tests.WeeklyPlan;

#pragma warning disable SA1601
public partial class WeeklyPlanTests
#pragma warning restore SA1601
{
	[Fact]
	public void SetFreetext_SetsContentType()
	{
		var plan = WeeklyPlanBuilder.A().Build();
		var label = FreetextLabel.From("Eating out");

		plan.SetFreetext(DayOfWeek.Monday, label);

		var monday = plan.Slots.First(slot => slot.Day == DayOfWeek.Monday);
		monday.ContentType.Should().Be(SlotContentType.Freetext);
		monday.FreetextLabel.Should().Be(label);
		monday.IsEmpty.Should().BeFalse();
		monday.Recipe.Should().BeNull();
	}

	[Fact]
	public void SetFreetext_EmptyLabel_ThrowsGuardException()
	{
		var plan = WeeklyPlanBuilder.A().Build();

		var act = () => plan.SetFreetext(DayOfWeek.Monday, FreetextLabel.From(string.Empty));

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void SetLeftover_SetsContentTypeAndSource()
	{
		var plan = WeeklyPlanBuilder.A().Build();
		var bolognese = Recipe("Bolognese");
		plan.AssignRecipe(DayOfWeek.Monday, bolognese);

		plan.SetLeftover(DayOfWeek.Wednesday, DayOfWeek.Monday, MealType.Dinner, bolognese.Title);

		var wednesday = plan.Slots.First(slot => slot.Day == DayOfWeek.Wednesday);
		wednesday.ContentType.Should().Be(SlotContentType.Leftover);
		wednesday.LeftoverSourceDay.Should().Be(DayOfWeek.Monday);
		wednesday.LeftoverSourceMealType.Should().Be(MealType.Dinner);
		wednesday.LeftoverLabel!.Value.Should().Contain("Bolognese");
		wednesday.IsEmpty.Should().BeFalse();
	}

	[Fact]
	public void SetLeftover_EmptyTitle_ThrowsGuardException()
	{
		var plan = WeeklyPlanBuilder.A().Build();

		var act = () => plan.SetLeftover(DayOfWeek.Wednesday, DayOfWeek.Monday, MealType.Dinner, SlotRecipeTitle.From(string.Empty));

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void AssignRecipe_SetsContentTypeToRecipe()
	{
		var plan = WeeklyPlanBuilder.A().Build();

		plan.AssignRecipe(DayOfWeek.Tuesday, Recipe("Steak"));

		plan.Slots.First(slot => slot.Day == DayOfWeek.Tuesday).ContentType.Should().Be(SlotContentType.Recipe);
	}

	[Fact]
	public void ClearSlot_ResetsContentTypeToEmpty()
	{
		var plan = WeeklyPlanBuilder.A().Build();
		plan.SetFreetext(DayOfWeek.Monday, FreetextLabel.From("Pizza night"));

		plan.ClearSlot(DayOfWeek.Monday);

		var monday = plan.Slots.First(slot => slot.Day == DayOfWeek.Monday);
		monday.ContentType.Should().Be(SlotContentType.Empty);
		monday.FreetextLabel.Should().BeNull();
		monday.IsEmpty.Should().BeTrue();
	}

	[Fact]
	public void AssignRecipe_ClearsFreetextAndLeftoverFields()
	{
		var plan = WeeklyPlanBuilder.A().Build();
		plan.SetFreetext(DayOfWeek.Monday, FreetextLabel.From("Eating out"));

		plan.AssignRecipe(DayOfWeek.Monday, Recipe());

		var monday = plan.Slots.First(slot => slot.Day == DayOfWeek.Monday);
		monday.ContentType.Should().Be(SlotContentType.Recipe);
		monday.FreetextLabel.Should().BeNull();
		monday.LeftoverSourceDay.Should().BeNull();
	}

	[Fact]
	public void Create_AllSlotsStartEmpty()
	{
		var plan = WeeklyPlanBuilder.A().Build();

		plan.Slots.Should().OnlyContain(s => s.ContentType == SlotContentType.Empty);
	}

	[Fact]
	public void SwapSlots_RecipeWithFreetext_SwapsAllContent()
	{
		var plan = WeeklyPlanBuilder.A().Build();
		var pasta = Recipe("Pasta");
		var eatingOut = FreetextLabel.From("Eating out");
		plan.AssignRecipe(DayOfWeek.Monday, pasta);
		plan.SetFreetext(DayOfWeek.Tuesday, eatingOut);

		plan.SwapSlots(DayOfWeek.Monday, DayOfWeek.Tuesday);

		var monday = plan.Slots.First(slot => slot.Day == DayOfWeek.Monday);
		monday.ContentType.Should().Be(SlotContentType.Freetext);
		monday.FreetextLabel.Should().Be(eatingOut);
		monday.Recipe.Should().BeNull();

		var tuesday = plan.Slots.First(slot => slot.Day == DayOfWeek.Tuesday);
		tuesday.ContentType.Should().Be(SlotContentType.Recipe);
		tuesday.Recipe!.Identifier.Should().Be(pasta.Identifier);
		tuesday.FreetextLabel.Should().BeNull();
	}

	[Fact]
	public void SwapSlots_RecipeWithLeftover_SwapsAllContent()
	{
		var plan = WeeklyPlanBuilder.A().Build();
		var bolognese = Recipe("Bolognese");
		plan.AssignRecipe(DayOfWeek.Monday, bolognese);
		plan.SetLeftover(DayOfWeek.Wednesday, DayOfWeek.Monday, MealType.Dinner, bolognese.Title);

		plan.SwapSlots(DayOfWeek.Monday, DayOfWeek.Wednesday);

		var monday = plan.Slots.First(slot => slot.Day == DayOfWeek.Monday);
		monday.ContentType.Should().Be(SlotContentType.Leftover);
		monday.LeftoverSourceDay.Should().Be(DayOfWeek.Monday);

		var wednesday = plan.Slots.First(slot => slot.Day == DayOfWeek.Wednesday);
		wednesday.ContentType.Should().Be(SlotContentType.Recipe);
		wednesday.Recipe!.Title.Should().Be(bolognese.Title);
	}

	[Fact]
	public void SwapSlots_FreetextWithEmpty_MovesContent()
	{
		var plan = WeeklyPlanBuilder.A().Build();
		var pizzaOrder = FreetextLabel.From("Pizza order");
		plan.SetFreetext(DayOfWeek.Monday, pizzaOrder);

		plan.SwapSlots(DayOfWeek.Monday, DayOfWeek.Tuesday);

		plan.Slots.First(slot => slot.Day == DayOfWeek.Monday).IsEmpty.Should().BeTrue();
		plan.Slots.First(slot => slot.Day == DayOfWeek.Tuesday).FreetextLabel.Should().Be(pizzaOrder);
	}
}
