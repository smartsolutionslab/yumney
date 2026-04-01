using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Common;

public class PageTests
{
    [Fact]
    public void From_ValidPage_CreatesInstance()
    {
        var page = Page.From(1);

        page.Value.Should().Be(1);
    }

    [Fact]
    public void From_Zero_ThrowsGuardException()
    {
        var act = () => Page.From(0);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void From_Negative_ThrowsGuardException()
    {
        var act = () => Page.From(-1);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void SkipCount_PageOne_ReturnsZero()
    {
        var page = Page.From(1);
        var pageSize = PageSize.From(20);

        page.SkipCount(pageSize).Should().Be(0);
    }

    [Fact]
    public void SkipCount_PageTwo_ReturnsPageSize()
    {
        var page = Page.From(2);
        var pageSize = PageSize.From(20);

        page.SkipCount(pageSize).Should().Be(20);
    }

    [Fact]
    public void SkipCount_PageThree_ReturnsTwicePageSize()
    {
        var page = Page.From(3);
        var pageSize = PageSize.From(20);

        page.SkipCount(pageSize).Should().Be(40);
    }
}
