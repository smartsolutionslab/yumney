using FluentAssertions;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Guards;
using Xunit;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.Tests.WeeklyPlan;

public class MealSlotIdentifierTests
{
	[Fact]
	public void New_CreatesNonEmptyGuid()
	{
		var id = MealSlotIdentifier.New();

		id.Value.Should().NotBe(Guid.Empty);
	}

	[Fact]
	public void From_ValidGuid_CreatesInstance()
	{
		var guid = Guid.NewGuid();
		var id = MealSlotIdentifier.From(guid);

		id.Value.Should().Be(guid);
	}

	[Fact]
	public void From_EmptyGuid_ThrowsGuardException()
	{
		var act = () => MealSlotIdentifier.From(Guid.Empty);

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void Equality_SameGuid_AreEqual()
	{
		var guid = Guid.NewGuid();
		var a = MealSlotIdentifier.From(guid);
		var b = MealSlotIdentifier.From(guid);

		a.Should().Be(b);
	}
}
