using FluentAssertions;
using Xunit;
using Yumney.Shared.Guards;

namespace Yumney.Shared.Tests.Guards;

public class EnsureTests
{
    [Fact]
    public void IsNotNull_WithNonNullValue_ShouldNotThrow()
    {
        var act = () => Ensure.That("hello").IsNotNull();

        act.Should().NotThrow();
    }

    [Fact]
    public void IsNotNull_WithNull_ShouldThrowGuardException()
    {
        string? value = null;

        var act = () => Ensure.That(value!).IsNotNull();

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void IsNotNullOrWhiteSpace_WithEmptyString_ShouldThrowGuardException()
    {
        var act = () => Ensure.That("").IsNotNullOrWhiteSpace();

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void HasMaxLength_WithTooLongString_ShouldThrowGuardException()
    {
        string value = new('x', 201);

        var act = () => Ensure.That(value).HasMaxLength(200);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void IsPositive_WithZero_ShouldThrowGuardException()
    {
        var act = () => Ensure.That(0).IsPositive();

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void IsPositive_WithPositiveValue_ShouldNotThrow()
    {
        var act = () => Ensure.That(5).IsPositive();

        act.Should().NotThrow();
    }

    [Fact]
    public void IsValidUrl_WithValidHttpsUrl_ShouldNotThrow()
    {
        var act = () => Ensure.That("https://example.com").IsValidUrl();

        act.Should().NotThrow();
    }

    [Fact]
    public void IsValidUrl_WithInvalidUrl_ShouldThrowGuardException()
    {
        var act = () => Ensure.That("not-a-url").IsValidUrl();

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void IsInRange_WithValueOutOfRange_ShouldThrowGuardException()
    {
        var act = () => Ensure.That(6).IsInRange(1, 5);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void AndReturn_ShouldReturnOriginalValue()
    {
        string result = Ensure.That("hello").IsNotNullOrWhiteSpace().AndReturn();

        result.Should().Be("hello");
    }
}
