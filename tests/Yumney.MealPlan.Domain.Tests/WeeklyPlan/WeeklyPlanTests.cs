using FluentAssertions;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;
using Xunit;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.Tests.WeeklyPlan;

public class WeeklyPlanTests
{
    private static readonly OwnerIdentifier TestOwner = OwnerIdentifier.From("user-123");

    [Fact]
    public void Create_ValidParams_Returns7EmptySlots()
    {
        var week = WeekIdentifier.From(2026, 15);
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, week);

        plan.Owner.Should().Be(TestOwner);
        plan.Week.Should().Be(week);
        plan.Slots.Should().HaveCount(7);
        plan.Slots.Should().OnlyContain(s => s.IsEmpty);
    }

    [Fact]
    public void Create_SetsDefaultServings()
    {
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 15), 6);

        plan.Slots.Should().OnlyContain(s => s.Servings == 6);
    }

    [Fact]
    public void AssignRecipe_FillsSlot()
    {
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 15));
        var recipeId = Guid.NewGuid();

        plan.AssignRecipe(DayOfWeek.Monday, recipeId, "Spaghetti Bolognese");

        var monday = plan.Slots.First(s => s.Day == DayOfWeek.Monday);
        monday.IsEmpty.Should().BeFalse();
        monday.RecipeIdentifier.Should().Be(recipeId);
        monday.RecipeTitle.Should().Be("Spaghetti Bolognese");
    }

    [Fact]
    public void AssignRecipe_WithServings_OverridesDefault()
    {
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 15));

        plan.AssignRecipe(DayOfWeek.Monday, Guid.NewGuid(), "Pasta", 8);

        plan.Slots.First(s => s.Day == DayOfWeek.Monday).Servings.Should().Be(8);
    }

    [Fact]
    public void ClearSlot_RemovesRecipe()
    {
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 15));
        plan.AssignRecipe(DayOfWeek.Wednesday, Guid.NewGuid(), "Chicken");

        plan.ClearSlot(DayOfWeek.Wednesday);

        plan.Slots.First(s => s.Day == DayOfWeek.Wednesday).IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void SwapSlots_SwapsTwoMeals()
    {
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 15));
        var recipeA = Guid.NewGuid();
        var recipeB = Guid.NewGuid();
        plan.AssignRecipe(DayOfWeek.Monday, recipeA, "Pasta");
        plan.AssignRecipe(DayOfWeek.Friday, recipeB, "Steak");

        plan.SwapSlots(DayOfWeek.Monday, DayOfWeek.Friday);

        plan.Slots.First(s => s.Day == DayOfWeek.Monday).RecipeTitle.Should().Be("Steak");
        plan.Slots.First(s => s.Day == DayOfWeek.Friday).RecipeTitle.Should().Be("Pasta");
    }

    [Fact]
    public void SwapSlots_WithOneEmpty_MovesRecipe()
    {
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 15));
        plan.AssignRecipe(DayOfWeek.Monday, Guid.NewGuid(), "Pasta");

        plan.SwapSlots(DayOfWeek.Monday, DayOfWeek.Tuesday);

        plan.Slots.First(s => s.Day == DayOfWeek.Monday).IsEmpty.Should().BeTrue();
        plan.Slots.First(s => s.Day == DayOfWeek.Tuesday).RecipeTitle.Should().Be("Pasta");
    }

    [Fact]
    public void AssignRecipe_EmptyTitle_ThrowsGuardException()
    {
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 15));

        var act = () => plan.AssignRecipe(DayOfWeek.Monday, Guid.NewGuid(), string.Empty);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void WeekIdentifier_CurrentReturnsValue()
    {
        var current = WeekIdentifier.Current();

        current.Year.Should().BeGreaterThan(2024);
        current.WeekNumber.Should().BeInRange(1, 53);
    }

    [Fact]
    public void WeekIdentifier_FromDate_CorrectWeek()
    {
        var week = WeekIdentifier.FromDate(new DateOnly(2026, 4, 13));

        week.Year.Should().Be(2026);
        week.WeekNumber.Should().Be(16);
    }

    [Fact]
    public void AssignRecipe_OverwritesExistingRecipe()
    {
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 15));
        plan.AssignRecipe(DayOfWeek.Monday, Guid.NewGuid(), "Pasta");

        var newRecipeId = Guid.NewGuid();
        plan.AssignRecipe(DayOfWeek.Monday, newRecipeId, "Steak");

        var monday = plan.Slots.First(s => s.Day == DayOfWeek.Monday);
        monday.RecipeIdentifier.Should().Be(newRecipeId);
        monday.RecipeTitle.Should().Be("Steak");
    }

    [Fact]
    public void SwapSlots_BothEmpty_NoOp()
    {
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 15));

        plan.SwapSlots(DayOfWeek.Monday, DayOfWeek.Tuesday);

        plan.Slots.First(s => s.Day == DayOfWeek.Monday).IsEmpty.Should().BeTrue();
        plan.Slots.First(s => s.Day == DayOfWeek.Tuesday).IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void ClearSlot_AlreadyEmpty_NoError()
    {
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 15));

        var act = () => plan.ClearSlot(DayOfWeek.Friday);

        act.Should().NotThrow();
    }

    [Fact]
    public void WeekIdentifier_ToString_ReturnsIsoFormat()
    {
        var week = WeekIdentifier.From(2026, 3);

        week.ToString().Should().Be("2026-W03");
    }

    [Fact]
    public void Slots_ContainAllSevenDays()
    {
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 15));

        var days = plan.Slots.Select(s => s.Day).ToList();
        days.Should().Contain(DayOfWeek.Monday);
        days.Should().Contain(DayOfWeek.Tuesday);
        days.Should().Contain(DayOfWeek.Wednesday);
        days.Should().Contain(DayOfWeek.Thursday);
        days.Should().Contain(DayOfWeek.Friday);
        days.Should().Contain(DayOfWeek.Saturday);
        days.Should().Contain(DayOfWeek.Sunday);
    }
}
