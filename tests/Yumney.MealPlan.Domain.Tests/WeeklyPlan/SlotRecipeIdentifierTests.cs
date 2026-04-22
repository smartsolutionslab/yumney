using FluentAssertions;
using SmartSolutionsLab.Yumney.MealPlan.Domain.WeeklyPlan;
using SmartSolutionsLab.Yumney.Shared.Guards;
using Xunit;

namespace SmartSolutionsLab.Yumney.MealPlan.Domain.Tests.WeeklyPlan;

public class SlotRecipeIdentifierTests
{
	[Fact]
	public void New_ReturnsVersion7Guid()
	{
		var id = SlotRecipeIdentifier.New();

		id.Value.Should().NotBe(Guid.Empty);
		id.Value.Version.Should().Be(7);
	}

	[Fact]
	public void New_EachCall_ReturnsDistinctGuid()
	{
		var a = SlotRecipeIdentifier.New();
		var b = SlotRecipeIdentifier.New();

		a.Should().NotBe(b);
	}

	[Fact]
	public void From_ValidGuid_CreatesInstance()
	{
		var guid = Guid.NewGuid();
		var id = SlotRecipeIdentifier.From(guid);

		id.Value.Should().Be(guid);
	}

	[Fact]
	public void From_EmptyGuid_ThrowsGuardException()
	{
		var act = () => SlotRecipeIdentifier.From(Guid.Empty);

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void Equality_SameGuid_AreEqual()
	{
		var guid = Guid.NewGuid();
		var a = SlotRecipeIdentifier.From(guid);
		var b = SlotRecipeIdentifier.From(guid);

		a.Should().Be(b);
	}
}
