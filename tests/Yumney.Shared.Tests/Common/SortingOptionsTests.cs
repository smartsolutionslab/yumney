using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Common;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Common;

public class SortingOptionsTests
{
    private enum TestSortField
    {
        Name,
        Date,
    }

    [Fact]
    public void Constructor_WithSortByAndDirection_StoresBothValues()
    {
        var sorting = new SortingOptions<TestSortField>(TestSortField.Name, SortDirection.Ascending);

        sorting.SortBy.Should().Be(TestSortField.Name);
        sorting.Direction.Should().Be(SortDirection.Ascending);
    }

    [Fact]
    public void Constructor_WithDescendingDirection_StoresDescending()
    {
        var sorting = new SortingOptions<TestSortField>(TestSortField.Date, SortDirection.Descending);

        sorting.SortBy.Should().Be(TestSortField.Date);
        sorting.Direction.Should().Be(SortDirection.Descending);
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var sorting1 = new SortingOptions<TestSortField>(TestSortField.Name, SortDirection.Ascending);
        var sorting2 = new SortingOptions<TestSortField>(TestSortField.Name, SortDirection.Ascending);

        sorting1.Should().Be(sorting2);
    }

    [Fact]
    public void Equality_DifferentSortBy_AreNotEqual()
    {
        var sorting1 = new SortingOptions<TestSortField>(TestSortField.Name, SortDirection.Ascending);
        var sorting2 = new SortingOptions<TestSortField>(TestSortField.Date, SortDirection.Ascending);

        sorting1.Should().NotBe(sorting2);
    }

    [Fact]
    public void Equality_DifferentDirection_AreNotEqual()
    {
        var sorting1 = new SortingOptions<TestSortField>(TestSortField.Name, SortDirection.Ascending);
        var sorting2 = new SortingOptions<TestSortField>(TestSortField.Name, SortDirection.Descending);

        sorting1.Should().NotBe(sorting2);
    }

    [Fact]
    public void Deconstruction_ReturnsFieldAndDirection()
    {
        var sorting = new SortingOptions<TestSortField>(TestSortField.Date, SortDirection.Descending);

        var (sortBy, direction) = sorting;

        sortBy.Should().Be(TestSortField.Date);
        direction.Should().Be(SortDirection.Descending);
    }
}
