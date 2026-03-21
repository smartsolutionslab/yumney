using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.Tests.ShoppingList;

public class RecipeReferenceTests
{
    [Fact]
    public void Constructor_ValidGuid_CreatesInstance()
    {
        var guid = Guid.NewGuid();

        var reference = new RecipeReference(guid);

        reference.Value.Should().Be(guid);
    }

    [Fact]
    public void Constructor_EmptyGuid_ThrowsGuardException()
    {
        var act = () => new RecipeReference(Guid.Empty);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void ToString_ReturnsGuidString()
    {
        var guid = Guid.NewGuid();

        var reference = new RecipeReference(guid);

        reference.ToString().Should().Be(guid.ToString());
    }

    [Fact]
    public void FromNullable_WithValue_ReturnsInstance()
    {
        var guid = Guid.NewGuid();

        var reference = RecipeReference.FromNullable(guid);

        reference.Should().NotBeNull();
        reference!.Value.Should().Be(guid);
    }

    [Fact]
    public void FromNullable_WithNull_ReturnsNull()
    {
        var reference = RecipeReference.FromNullable(null);

        reference.Should().BeNull();
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var guid = Guid.NewGuid();

        var a = new RecipeReference(guid);
        var b = new RecipeReference(guid);

        a.Should().Be(b);
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = new RecipeReference(Guid.NewGuid());
        var b = new RecipeReference(Guid.NewGuid());

        a.Should().NotBe(b);
    }
}
