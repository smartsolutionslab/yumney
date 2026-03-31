using FluentAssertions;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Guards;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Recipe;

public class SearchTermTests
{
    [Fact]
    public void Constructor_ValidInput_CreatesInstance()
    {
        var searchTerm = SearchTerm.From("pasta");

        searchTerm.Value.Should().Be("pasta");
    }

    [Fact]
    public void Constructor_TrimsWhitespace()
    {
        var searchTerm = SearchTerm.From("  pasta  ");

        searchTerm.Value.Should().Be("pasta");
    }

    [Fact]
    public void Constructor_AtMaxLength_CreatesInstance()
    {
        var value = new string('a', 200);

        var searchTerm = SearchTerm.From(value);

        searchTerm.Value.Should().HaveLength(200);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrWhitespace_ThrowsGuardException(string? value)
    {
        var act = () => SearchTerm.From(value!);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Constructor_ExceedsMaxLength_ThrowsGuardException()
    {
        var value = new string('a', 201);

        var act = () => SearchTerm.From(value);

        act.Should().Throw<GuardException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FromNullable_NullOrWhitespace_ReturnsNull(string? value)
    {
        var result = SearchTerm.FromNullable(value);

        result.Should().BeNull();
    }

    [Fact]
    public void FromNullable_ValidInput_ReturnsSearchTerm()
    {
        var result = SearchTerm.FromNullable("pasta");

        result.Should().NotBeNull();
        result!.Value.Should().Be("pasta");
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var searchTerm = SearchTerm.From("pasta");

        searchTerm.ToString().Should().Be("pasta");
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var searchTerm1 = SearchTerm.From("pasta");
        var searchTerm2 = SearchTerm.From("pasta");

        searchTerm1.Should().Be(searchTerm2);
    }
}
