using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.Users.Domain.UserActivity;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Domain.Tests.UserActivity;

public class RecipeTitleSnapshotTests
{
    [Fact]
    public void From_ValidValue_CreatesInstance()
    {
        var snapshot = RecipeTitleSnapshot.From("Pasta Carbonara");

        snapshot.Value.Should().Be("Pasta Carbonara");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void From_NullOrWhitespace_ThrowsGuardException(string? value)
    {
        var act = () => RecipeTitleSnapshot.From(value!);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void From_ExceedsMaxLength_ThrowsGuardException()
    {
        var longTitle = new string('x', 201);

        var act = () => RecipeTitleSnapshot.From(longTitle);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void FromNullable_Null_ReturnsNull()
    {
        var result = RecipeTitleSnapshot.FromNullable(null);

        result.Should().BeNull();
    }

    [Fact]
    public void FromNullable_ValidValue_ReturnsInstance()
    {
        var result = RecipeTitleSnapshot.FromNullable("Steak");

        result.Should().NotBeNull();
        result!.Value.Should().Be("Steak");
    }

    [Fact]
    public void ImplicitConversion_ReturnsString()
    {
        string value = RecipeTitleSnapshot.From("Pizza");

        value.Should().Be("Pizza");
    }
}
