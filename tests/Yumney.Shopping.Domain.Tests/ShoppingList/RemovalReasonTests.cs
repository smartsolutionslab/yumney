using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.Tests.ShoppingList;

public class RemovalReasonTests
{
    [Fact]
    public void From_ValidValue_CreatesInstance()
    {
        var reason = RemovalReason.From("spoiled");

        reason.Value.Should().Be("spoiled");
    }

    [Fact]
    public void From_Trims_WhiteSpace()
    {
        var reason = RemovalReason.From("  expired  ");

        reason.Value.Should().Be("expired");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void From_EmptyValue_ThrowsGuardException(string? value)
    {
        var act = () => RemovalReason.From(value!);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void From_TooLong_ThrowsGuardException()
    {
        var act = () => RemovalReason.From(new string('a', 501));

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void FromNullable_WithValue_CreatesInstance()
    {
        var reason = RemovalReason.FromNullable("not needed");

        reason.Should().NotBeNull();
        reason!.Value.Should().Be("not needed");
    }

    [Fact]
    public void FromNullable_Null_ReturnsNull()
    {
        var reason = RemovalReason.FromNullable(null);

        reason.Should().BeNull();
    }

    [Fact]
    public void FromNullable_Empty_ReturnsNull()
    {
        var reason = RemovalReason.FromNullable(string.Empty);

        reason.Should().BeNull();
    }

    [Fact]
    public void ImplicitConversion_ReturnsValue()
    {
        string result = RemovalReason.From("wrong item");

        result.Should().Be("wrong item");
    }
}
