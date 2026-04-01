using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Common;

public class GuidIdentifierTests
{
    private sealed record TestIdentifier : GuidIdentifier
    {
        public TestIdentifier(Guid value)
            : base(value)
        {
        }

        public static TestIdentifier From(Guid value) => new(value);

        public static TestIdentifier New() => new(Guid.NewGuid());
    }

    [Fact]
    public void Constructor_ValidGuid_CreatesInstance()
    {
        var guid = Guid.NewGuid();

        var identifier = TestIdentifier.From(guid);

        identifier.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_EmptyGuid_ThrowsGuardException()
    {
        var act = () => TestIdentifier.From(Guid.Empty);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void ToString_ReturnsGuidString()
    {
        var guid = Guid.NewGuid();
        var identifier = TestIdentifier.From(guid);

        var result = identifier.ToString();

        result.Should().Be(guid.ToString());
    }

    [Fact]
    public void Equality_SameGuid_AreEqual()
    {
        var guid = Guid.NewGuid();
        var first = TestIdentifier.From(guid);
        var second = TestIdentifier.From(guid);

        first.Should().Be(second);
    }

    [Fact]
    public void Equality_DifferentGuid_AreNotEqual()
    {
        var first = TestIdentifier.New();
        var second = TestIdentifier.New();

        first.Should().NotBe(second);
    }

    [Fact]
    public void Value_ReturnsProvidedGuid()
    {
        var guid = Guid.NewGuid();

        var identifier = TestIdentifier.From(guid);

        identifier.Value.Should().Be(guid);
    }
}
