using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.Users.Domain.UserActivity;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Domain.Tests.UserActivity;

public class RecipeIdentifierSnapshotTests
{
	[Fact]
	public void From_ValidGuid_CreatesInstance()
	{
		var guid = Guid.NewGuid();
		var snapshot = RecipeIdentifierSnapshot.From(guid);

		snapshot.Value.Should().Be(guid);
	}

	[Fact]
	public void From_EmptyGuid_ThrowsGuardException()
	{
		var act = () => RecipeIdentifierSnapshot.From(Guid.Empty);

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void FromNullable_Null_ReturnsNull()
	{
		var result = RecipeIdentifierSnapshot.FromNullable(null);

		result.Should().BeNull();
	}

	[Fact]
	public void FromNullable_ValidGuid_ReturnsInstance()
	{
		var guid = Guid.NewGuid();
		var result = RecipeIdentifierSnapshot.FromNullable(guid);

		result.Should().NotBeNull();
		result!.Value.Should().Be(guid);
	}

	[Fact]
	public void ImplicitConversion_ReturnsGuid()
	{
		var guid = Guid.NewGuid();
		Guid value = RecipeIdentifierSnapshot.From(guid);

		value.Should().Be(guid);
	}
}
