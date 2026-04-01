using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Common;

public class PageSizeTests
{
    [Fact]
    public void From_ValidSize_CreatesInstance()
    {
        var pageSize = PageSize.From(20);

        pageSize.Value.Should().Be(20);
    }

    [Fact]
    public void From_Zero_ThrowsGuardException()
    {
        var act = () => PageSize.From(0);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void From_Negative_ThrowsGuardException()
    {
        var act = () => PageSize.From(-1);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void From_ExceedsMax_ThrowsGuardException()
    {
        var act = () => PageSize.From(101);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void From_AtMin_CreatesInstance()
    {
        var pageSize = PageSize.From(1);

        pageSize.Value.Should().Be(1);
    }

    [Fact]
    public void From_AtMax_CreatesInstance()
    {
        var pageSize = PageSize.From(100);

        pageSize.Value.Should().Be(100);
    }
}
