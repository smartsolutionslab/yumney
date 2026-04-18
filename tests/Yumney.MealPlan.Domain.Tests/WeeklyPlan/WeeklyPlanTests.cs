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

        plan.Slots.Should().OnlyContain(s => s.Servings.Value == 6);
    }

    [Fact]
    public void AssignRecipe_FillsSlot()
    {
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 15));
        var recipeId = Guid.NewGuid();

        plan.AssignRecipe(DayOfWeek.Monday, SlotRecipeReference.From(recipeId, "Spaghetti Bolognese"));

        var monday = plan.Slots.First(s => s.Day == DayOfWeek.Monday);
        monday.IsEmpty.Should().BeFalse();
        monday.Recipe!.RecipeIdentifier.Should().Be(recipeId);
        monday.Recipe.Title.Should().Be("Spaghetti Bolognese");
    }

    [Fact]
    public void AssignRecipe_WithServings_OverridesDefault()
    {
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 15));

        plan.AssignRecipe(DayOfWeek.Monday, SlotRecipeReference.From(Guid.NewGuid(), "Pasta"), servings: 8);

        plan.Slots.First(s => s.Day == DayOfWeek.Monday).Servings.Value.Should().Be(8);
    }

    [Fact]
    public void ClearSlot_RemovesRecipe()
    {
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 15));
        plan.AssignRecipe(DayOfWeek.Wednesday, SlotRecipeReference.From(Guid.NewGuid(), "Chicken"));

        plan.ClearSlot(DayOfWeek.Wednesday);

        plan.Slots.First(s => s.Day == DayOfWeek.Wednesday).IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void SwapSlots_SwapsTwoMeals()
    {
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 15));
        var recipeA = Guid.NewGuid();
        var recipeB = Guid.NewGuid();
        plan.AssignRecipe(DayOfWeek.Monday, SlotRecipeReference.From(recipeA, "Pasta"));
        plan.AssignRecipe(DayOfWeek.Friday, SlotRecipeReference.From(recipeB, "Steak"));

        plan.SwapSlots(DayOfWeek.Monday, DayOfWeek.Friday);

        plan.Slots.First(s => s.Day == DayOfWeek.Monday).Recipe!.Title.Should().Be("Steak");
        plan.Slots.First(s => s.Day == DayOfWeek.Friday).Recipe!.Title.Should().Be("Pasta");
    }

    [Fact]
    public void SwapSlots_WithOneEmpty_MovesRecipe()
    {
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 15));
        plan.AssignRecipe(DayOfWeek.Monday, SlotRecipeReference.From(Guid.NewGuid(), "Pasta"));

        plan.SwapSlots(DayOfWeek.Monday, DayOfWeek.Tuesday);

        plan.Slots.First(s => s.Day == DayOfWeek.Monday).IsEmpty.Should().BeTrue();
        plan.Slots.First(s => s.Day == DayOfWeek.Tuesday).Recipe!.Title.Should().Be("Pasta");
    }

    [Fact]
    public void AssignRecipe_EmptyTitle_ThrowsGuardException()
    {
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 15));

        var act = () => plan.AssignRecipe(DayOfWeek.Monday, SlotRecipeReference.From(Guid.NewGuid(), string.Empty));

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
        plan.AssignRecipe(DayOfWeek.Monday, SlotRecipeReference.From(Guid.NewGuid(), "Pasta"));

        var newRecipeId = Guid.NewGuid();
        plan.AssignRecipe(DayOfWeek.Monday, SlotRecipeReference.From(newRecipeId, "Steak"));

        var monday = plan.Slots.First(s => s.Day == DayOfWeek.Monday);
        monday.Recipe!.RecipeIdentifier.Should().Be(newRecipeId);
        monday.Recipe.Title.Should().Be("Steak");
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

    [Fact]
    public void Create_DefaultMode_AllSlotsDinner()
    {
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 15));

        plan.IsExtendedMode.Should().BeFalse();
        plan.Slots.Should().OnlyContain(s => s.MealType == MealType.Dinner);
    }

    [Fact]
    public void EnableExtendedMode_Adds21Slots()
    {
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 15));

        plan.EnableExtendedMode();

        plan.IsExtendedMode.Should().BeTrue();
        plan.Slots.Should().HaveCount(21);
    }

    [Fact]
    public void EnableExtendedMode_PreservesDinnerRecipes()
    {
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 15));
        plan.AssignRecipe(DayOfWeek.Monday, SlotRecipeReference.From(Guid.NewGuid(), "Pasta"));

        plan.EnableExtendedMode();

        plan.Slots.First(s => s.Day == DayOfWeek.Monday && s.MealType == MealType.Dinner).Recipe!.Title.Should().Be("Pasta");
    }

    [Fact]
    public void EnableExtendedMode_AlreadyExtended_NoOp()
    {
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 15));
        plan.EnableExtendedMode();

        plan.EnableExtendedMode();

        plan.Slots.Should().HaveCount(21);
    }

    [Fact]
    public void DisableExtendedMode_ShowsOnlyDinner()
    {
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 15));
        plan.EnableExtendedMode();

        plan.DisableExtendedMode();

        plan.IsExtendedMode.Should().BeFalse();
        plan.GetVisibleSlots().Should().HaveCount(7);
        plan.GetVisibleSlots().Should().OnlyContain(s => s.MealType == MealType.Dinner);
    }

    [Fact]
    public void DisableExtendedMode_PreservesAllData()
    {
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 15));
        plan.EnableExtendedMode();
        plan.AssignRecipe(DayOfWeek.Monday, SlotRecipeReference.From(Guid.NewGuid(), "Cereal"), MealType.Breakfast);

        plan.DisableExtendedMode();

        plan.Slots.Should().HaveCount(21);
        plan.Slots.First(s => s.Day == DayOfWeek.Monday && s.MealType == MealType.Breakfast).Recipe!.Title.Should().Be("Cereal");
    }

    [Fact]
    public void AssignRecipe_BreakfastSlot_InExtendedMode()
    {
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 15));
        plan.EnableExtendedMode();

        plan.AssignRecipe(DayOfWeek.Tuesday, SlotRecipeReference.From(Guid.NewGuid(), "Pancakes"), MealType.Breakfast);

        plan.Slots.First(s => s.Day == DayOfWeek.Tuesday && s.MealType == MealType.Breakfast).Recipe!.Title.Should().Be("Pancakes");
    }

    [Fact]
    public void GetVisibleSlots_ExtendedMode_ReturnsAll21()
    {
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 15));
        plan.EnableExtendedMode();

        plan.GetVisibleSlots().Should().HaveCount(21);
    }

    [Fact]
    public void GetVisibleSlots_DefaultMode_Returns7()
    {
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 15));

        plan.GetVisibleSlots().Should().HaveCount(7);
    }

    [Fact]
    public void AssignRecipe_BreakfastInDefaultMode_ThrowsEntityNotFoundException()
    {
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 15));

        var act = () => plan.AssignRecipe(DayOfWeek.Monday, SlotRecipeReference.From(Guid.NewGuid(), "Cereal"), MealType.Breakfast);

        act.Should().Throw<EntityNotFoundException>();
    }

    [Fact]
    public void ClearSlot_BreakfastInDefaultMode_ThrowsEntityNotFoundException()
    {
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 15));

        var act = () => plan.ClearSlot(DayOfWeek.Monday, MealType.Breakfast);

        act.Should().Throw<EntityNotFoundException>();
    }

    [Fact]
    public void SwapSlots_BreakfastInExtendedMode_Works()
    {
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 15));
        plan.EnableExtendedMode();
        plan.AssignRecipe(DayOfWeek.Monday, SlotRecipeReference.From(Guid.NewGuid(), "Pancakes"), MealType.Breakfast);

        plan.SwapSlots(DayOfWeek.Monday, DayOfWeek.Tuesday, MealType.Breakfast);

        plan.Slots.First(s => s.Day == DayOfWeek.Monday && s.MealType == MealType.Breakfast).IsEmpty.Should().BeTrue();
        plan.Slots.First(s => s.Day == DayOfWeek.Tuesday && s.MealType == MealType.Breakfast).Recipe!.Title.Should().Be("Pancakes");
    }

    [Fact]
    public void DisableExtendedMode_NotExtended_NoOp()
    {
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 15));

        plan.DisableExtendedMode();

        plan.IsExtendedMode.Should().BeFalse();
        plan.Slots.Should().HaveCount(7);
    }

    [Fact]
    public void SetFreetext_SetsContentType()
    {
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 15));

        plan.SetFreetext(DayOfWeek.Monday, FreetextLabel.From("Eating out"));

        var monday = plan.Slots.First(s => s.Day == DayOfWeek.Monday);
        monday.ContentType.Should().Be(SlotContentType.Freetext);
        monday.FreetextLabel!.Value.Should().Be("Eating out");
        monday.IsEmpty.Should().BeFalse();
        monday.Recipe.Should().BeNull();
    }

    [Fact]
    public void SetFreetext_EmptyLabel_ThrowsGuardException()
    {
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 15));

        var act = () => plan.SetFreetext(DayOfWeek.Monday, FreetextLabel.From(string.Empty));

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void SetLeftover_SetsContentTypeAndSource()
    {
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 15));
        plan.AssignRecipe(DayOfWeek.Monday, SlotRecipeReference.From(Guid.NewGuid(), "Bolognese"));

        plan.SetLeftover(DayOfWeek.Wednesday, DayOfWeek.Monday, MealType.Dinner, "Bolognese");

        var wednesday = plan.Slots.First(s => s.Day == DayOfWeek.Wednesday);
        wednesday.ContentType.Should().Be(SlotContentType.Leftover);
        wednesday.LeftoverSourceDay.Should().Be(DayOfWeek.Monday);
        wednesday.LeftoverSourceMealType.Should().Be(MealType.Dinner);
        wednesday.LeftoverLabel!.Value.Should().Contain("Bolognese");
        wednesday.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void SetLeftover_EmptyTitle_ThrowsGuardException()
    {
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 15));

        var act = () => plan.SetLeftover(DayOfWeek.Wednesday, DayOfWeek.Monday, MealType.Dinner, string.Empty);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void AssignRecipe_SetsContentTypeToRecipe()
    {
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 15));

        plan.AssignRecipe(DayOfWeek.Tuesday, SlotRecipeReference.From(Guid.NewGuid(), "Steak"));

        plan.Slots.First(s => s.Day == DayOfWeek.Tuesday).ContentType.Should().Be(SlotContentType.Recipe);
    }

    [Fact]
    public void ClearSlot_ResetsContentTypeToEmpty()
    {
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 15));
        plan.SetFreetext(DayOfWeek.Monday, FreetextLabel.From("Pizza night"));

        plan.ClearSlot(DayOfWeek.Monday);

        var monday = plan.Slots.First(s => s.Day == DayOfWeek.Monday);
        monday.ContentType.Should().Be(SlotContentType.Empty);
        monday.FreetextLabel.Should().BeNull();
        monday.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void AssignRecipe_ClearsFreetextAndLeftoverFields()
    {
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 15));
        plan.SetFreetext(DayOfWeek.Monday, FreetextLabel.From("Eating out"));

        plan.AssignRecipe(DayOfWeek.Monday, SlotRecipeReference.From(Guid.NewGuid(), "Pasta"));

        var monday = plan.Slots.First(s => s.Day == DayOfWeek.Monday);
        monday.ContentType.Should().Be(SlotContentType.Recipe);
        monday.FreetextLabel.Should().BeNull();
        monday.LeftoverSourceDay.Should().BeNull();
    }

    [Fact]
    public void Create_AllSlotsStartEmpty()
    {
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 15));

        plan.Slots.Should().OnlyContain(s => s.ContentType == SlotContentType.Empty);
    }

    [Fact]
    public void SwapSlots_RecipeWithFreetext_SwapsAllContent()
    {
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 15));
        var recipeId = Guid.NewGuid();
        plan.AssignRecipe(DayOfWeek.Monday, SlotRecipeReference.From(recipeId, "Pasta"));
        plan.SetFreetext(DayOfWeek.Tuesday, FreetextLabel.From("Eating out"));

        plan.SwapSlots(DayOfWeek.Monday, DayOfWeek.Tuesday);

        var monday = plan.Slots.First(s => s.Day == DayOfWeek.Monday);
        monday.ContentType.Should().Be(SlotContentType.Freetext);
        monday.FreetextLabel!.Value.Should().Be("Eating out");
        monday.Recipe.Should().BeNull();

        var tuesday = plan.Slots.First(s => s.Day == DayOfWeek.Tuesday);
        tuesday.ContentType.Should().Be(SlotContentType.Recipe);
        tuesday.Recipe!.RecipeIdentifier.Should().Be(recipeId);
        tuesday.FreetextLabel.Should().BeNull();
    }

    [Fact]
    public void SwapSlots_RecipeWithLeftover_SwapsAllContent()
    {
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 15));
        plan.AssignRecipe(DayOfWeek.Monday, SlotRecipeReference.From(Guid.NewGuid(), "Bolognese"));
        plan.SetLeftover(DayOfWeek.Wednesday, DayOfWeek.Monday, MealType.Dinner, "Bolognese");

        plan.SwapSlots(DayOfWeek.Monday, DayOfWeek.Wednesday);

        var monday = plan.Slots.First(s => s.Day == DayOfWeek.Monday);
        monday.ContentType.Should().Be(SlotContentType.Leftover);
        monday.LeftoverSourceDay.Should().Be(DayOfWeek.Monday);

        var wednesday = plan.Slots.First(s => s.Day == DayOfWeek.Wednesday);
        wednesday.ContentType.Should().Be(SlotContentType.Recipe);
        wednesday.Recipe!.Title.Should().Be("Bolognese");
    }

    [Fact]
    public void SwapSlots_FreetextWithEmpty_MovesContent()
    {
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 15));
        plan.SetFreetext(DayOfWeek.Monday, FreetextLabel.From("Pizza order"));

        plan.SwapSlots(DayOfWeek.Monday, DayOfWeek.Tuesday);

        plan.Slots.First(s => s.Day == DayOfWeek.Monday).IsEmpty.Should().BeTrue();
        plan.Slots.First(s => s.Day == DayOfWeek.Tuesday).FreetextLabel!.Value.Should().Be("Pizza order");
    }

    [Fact]
    public void AdjustServings_ChangesSlotServings()
    {
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 15), 4);
        plan.AssignRecipe(DayOfWeek.Monday, SlotRecipeReference.From(Guid.NewGuid(), "Pasta"));

        plan.AdjustServings(DayOfWeek.Monday, SlotServings.From(8));

        plan.Slots.First(s => s.Day == DayOfWeek.Monday).Servings.Value.Should().Be(8);
    }

    [Fact]
    public void AdjustServings_DefaultServingsPreservedOnOtherSlots()
    {
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 15), 4);

        plan.AdjustServings(DayOfWeek.Monday, SlotServings.From(6));

        plan.Slots.First(s => s.Day == DayOfWeek.Tuesday).Servings.Value.Should().Be(4);
    }

    [Fact]
    public void SwapSlots_PreservesServings()
    {
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 15), 4);
        plan.AssignRecipe(DayOfWeek.Monday, SlotRecipeReference.From(Guid.NewGuid(), "Pasta"), servings: 8);

        plan.SwapSlots(DayOfWeek.Monday, DayOfWeek.Tuesday);

        plan.Slots.First(s => s.Day == DayOfWeek.Tuesday).Servings.Value.Should().Be(8);
        plan.Slots.First(s => s.Day == DayOfWeek.Monday).Servings.Value.Should().Be(4);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AdjustServings_ZeroOrNegative_ThrowsGuardException(int servings)
    {
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 15));

        var act = () => plan.AdjustServings(DayOfWeek.Monday, SlotServings.From(servings));

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void MarkAsCooked_SetsState()
    {
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 15));
        plan.AssignRecipe(DayOfWeek.Monday, SlotRecipeReference.From(Guid.NewGuid(), "Pasta"));

        plan.MarkAsCooked(DayOfWeek.Monday);

        plan.Slots.First(s => s.Day == DayOfWeek.Monday).State.Should().Be(MealState.Cooked);
    }

    [Fact]
    public void MarkAsSkipped_SetsState()
    {
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 15));
        plan.AssignRecipe(DayOfWeek.Monday, SlotRecipeReference.From(Guid.NewGuid(), "Pasta"));

        plan.MarkAsSkipped(DayOfWeek.Monday);

        plan.Slots.First(s => s.Day == DayOfWeek.Monday).State.Should().Be(MealState.Skipped);
    }

    [Fact]
    public void ResetToPlanned_ResetsState()
    {
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 15));
        plan.AssignRecipe(DayOfWeek.Monday, SlotRecipeReference.From(Guid.NewGuid(), "Pasta"));
        plan.MarkAsCooked(DayOfWeek.Monday);

        plan.ResetToPlanned(DayOfWeek.Monday);

        plan.Slots.First(s => s.Day == DayOfWeek.Monday).State.Should().Be(MealState.Planned);
    }

    [Fact]
    public void Create_AllSlotsStartAsPlanned()
    {
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 15));

        plan.Slots.Should().OnlyContain(s => s.State == MealState.Planned);
    }

    [Fact]
    public void GetUnconfirmedPastMeals_ReturnsOnlyPlannedRecipesBeforeToday()
    {
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 15));
        plan.AssignRecipe(DayOfWeek.Monday, SlotRecipeReference.From(Guid.NewGuid(), "Pasta"));
        plan.AssignRecipe(DayOfWeek.Tuesday, SlotRecipeReference.From(Guid.NewGuid(), "Steak"));
        plan.MarkAsCooked(DayOfWeek.Monday);

        var unconfirmed = plan.GetUnconfirmedPastMeals(DayOfWeek.Wednesday);

        unconfirmed.Should().HaveCount(1);
        unconfirmed[0].Day.Should().Be(DayOfWeek.Tuesday);
    }

    [Fact]
    public void GetUnconfirmedPastMeals_ExcludesFreetextAndLeftover()
    {
        var plan = Domain.WeeklyPlan.WeeklyPlan.Create(TestOwner, WeekIdentifier.From(2026, 15));
        plan.SetFreetext(DayOfWeek.Monday, FreetextLabel.From("Eating out"));
        plan.AssignRecipe(DayOfWeek.Tuesday, SlotRecipeReference.From(Guid.NewGuid(), "Steak"));

        var unconfirmed = plan.GetUnconfirmedPastMeals(DayOfWeek.Wednesday);

        unconfirmed.Should().HaveCount(1);
        unconfirmed[0].Recipe!.Title.Should().Be("Steak");
    }
}
