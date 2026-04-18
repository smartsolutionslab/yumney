using FluentAssertions;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Domain.Tests.AppUserProfile;

public class DietaryProfileTests
{
	[Fact]
	public void Empty_StaticInstance_IsEmpty()
	{
		DietaryProfile.Empty.IsEmpty.Should().BeTrue();
		DietaryProfile.Empty.DietaryType.Should().BeNull();
		DietaryProfile.Empty.Restrictions.Should().BeEmpty();
		DietaryProfile.Empty.BalanceGoals.IsEmpty.Should().BeTrue();
		DietaryProfile.Empty.CookingEffort.Should().BeNull();
	}

	[Fact]
	public void From_AllValues_CreatesInstance()
	{
		var profile = DietaryProfile.From(
			DietaryType.Vegetarian,
			[DietaryRestriction.GlutenFree, DietaryRestriction.LactoseFree],
			WeeklyBalanceGoals.From(3, 0),
			CookingEffortPreference.QuickWeekdays);

		profile.DietaryType.Should().Be(DietaryType.Vegetarian);
		profile.Restrictions.Should().HaveCount(2);
		profile.BalanceGoals.MinVeggieMeals.Should().Be(3);
		profile.CookingEffort.Should().Be(CookingEffortPreference.QuickWeekdays);
		profile.IsEmpty.Should().BeFalse();
	}

	[Fact]
	public void From_OnlyDietaryType_IsNotEmpty()
	{
		var profile = DietaryProfile.From(
			DietaryType.Vegan,
			[],
			WeeklyBalanceGoals.None,
			null);

		profile.IsEmpty.Should().BeFalse();
	}

	[Fact]
	public void From_OnlyRestrictions_IsNotEmpty()
	{
		var profile = DietaryProfile.From(
			null,
			[DietaryRestriction.NutAllergy],
			WeeklyBalanceGoals.None,
			null);

		profile.IsEmpty.Should().BeFalse();
	}

	[Fact]
	public void From_OnlyBalanceGoals_IsNotEmpty()
	{
		var profile = DietaryProfile.From(
			null,
			[],
			WeeklyBalanceGoals.From(2, null),
			null);

		profile.IsEmpty.Should().BeFalse();
	}

	[Fact]
	public void Equality_SameValues_AreEqual()
	{
		var p1 = DietaryProfile.From(DietaryType.Omnivore, [], WeeklyBalanceGoals.None, null);
		var p2 = DietaryProfile.From(DietaryType.Omnivore, [], WeeklyBalanceGoals.None, null);

		p1.Should().Be(p2);
	}

	[Fact]
	public void Equality_SameRestrictions_AreEqual()
	{
		var p1 = DietaryProfile.From(
			DietaryType.Vegetarian,
			[DietaryRestriction.GlutenFree, DietaryRestriction.LactoseFree],
			WeeklyBalanceGoals.From(2, 3),
			CookingEffortPreference.Balanced);
		var p2 = DietaryProfile.From(
			DietaryType.Vegetarian,
			[DietaryRestriction.GlutenFree, DietaryRestriction.LactoseFree],
			WeeklyBalanceGoals.From(2, 3),
			CookingEffortPreference.Balanced);

		p1.Should().Be(p2);
	}
}
