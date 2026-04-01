using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Common;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Common;

public class StringExtensionsTests
{
    [Fact]
    public void HasValue_NonEmpty_ReturnsTrue()
    {
        "hello".HasValue().Should().BeTrue();
    }

    [Fact]
    public void HasValue_Null_ReturnsFalse()
    {
        ((string?)null).HasValue().Should().BeFalse();
    }

    [Fact]
    public void HasValue_Empty_ReturnsFalse()
    {
        string.Empty.HasValue().Should().BeFalse();
    }

    [Fact]
    public void HasValue_Whitespace_ReturnsFalse()
    {
        "   ".HasValue().Should().BeFalse();
    }
}
