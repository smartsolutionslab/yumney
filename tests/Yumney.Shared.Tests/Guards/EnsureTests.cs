using FluentAssertions;
using Xunit;
using Yumney.Shared.Guards;

namespace Yumney.Shared.Tests.Guards;

public class EnsureTests
{
    [Fact]
    public void IsNotNullOrWhiteSpace_ValidString_DoesNotThrow()
    {
        var act = () => Ensure.That("hello").IsNotNullOrWhiteSpace();

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsNotNullOrWhiteSpace_InvalidString_ThrowsGuardException(string? value)
    {
        var act = () => Ensure.That(value!).IsNotNullOrWhiteSpace();

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void HasMaxLength_WithinLimit_DoesNotThrow()
    {
        var act = () => Ensure.That("hello").HasMaxLength(10);

        act.Should().NotThrow();
    }

    [Fact]
    public void HasMaxLength_ExceedsLimit_ThrowsGuardException()
    {
        var act = () => Ensure.That("hello world").HasMaxLength(5);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void HasMinLength_MeetsMinimum_DoesNotThrow()
    {
        var act = () => Ensure.That("hello").HasMinLength(3);

        act.Should().NotThrow();
    }

    [Fact]
    public void HasMinLength_BelowMinimum_ThrowsGuardException()
    {
        var act = () => Ensure.That("hi").HasMinLength(3);

        act.Should().Throw<GuardException>();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void IsPositive_Int_PositiveValue_DoesNotThrow(int value)
    {
        var act = () => Ensure.That(value).IsPositive();

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void IsPositive_Int_NonPositiveValue_ThrowsGuardException(int value)
    {
        var act = () => Ensure.That(value).IsPositive();

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void IsPositive_Decimal_PositiveValue_DoesNotThrow()
    {
        var act = () => Ensure.That(1.5m).IsPositive();

        act.Should().NotThrow();
    }

    [Fact]
    public void IsPositive_Decimal_ZeroValue_ThrowsGuardException()
    {
        var act = () => Ensure.That(0m).IsPositive();

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void IsNotNegative_Int_ZeroValue_DoesNotThrow()
    {
        var act = () => Ensure.That(0).IsNotNegative();

        act.Should().NotThrow();
    }

    [Fact]
    public void IsNotNegative_Int_NegativeValue_ThrowsGuardException()
    {
        var act = () => Ensure.That(-1).IsNotNegative();

        act.Should().Throw<GuardException>();
    }

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("http://example.com/path")]
    [InlineData("https://example.com/path?query=1")]
    public void IsValidUrl_ValidUrl_DoesNotThrow(string url)
    {
        var act = () => Ensure.That(url).IsValidUrl();

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com")]
    public void IsValidUrl_InvalidUrl_ThrowsGuardException(string? url)
    {
        var act = () => Ensure.That(url!).IsValidUrl();

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void IsNotNull_ValidReference_DoesNotThrow()
    {
        var act = () => Ensure.That("hello").IsNotNull();

        act.Should().NotThrow();
    }

    [Fact]
    public void IsNotNull_NullReference_ThrowsGuardException()
    {
        string? value = null;

        var act = () => Ensure.That(value!).IsNotNull();

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void IsNotEmpty_Guid_ValidGuid_DoesNotThrow()
    {
        var act = () => Ensure.That(Guid.NewGuid()).IsNotEmpty();

        act.Should().NotThrow();
    }

    [Fact]
    public void IsNotEmpty_Guid_EmptyGuid_ThrowsGuardException()
    {
        var act = () => Ensure.That(Guid.Empty).IsNotEmpty();

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void IsNotEmpty_Collection_NonEmptyCollection_DoesNotThrow()
    {
        IReadOnlyCollection<int> items = new[] { 1, 2, 3 };

        var act = () => Ensure.That(items).IsNotEmpty();

        act.Should().NotThrow();
    }

    [Fact]
    public void IsNotEmpty_Collection_EmptyCollection_ThrowsGuardException()
    {
        IReadOnlyCollection<int> items = Array.Empty<int>();

        var act = () => Ensure.That(items).IsNotEmpty();

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void IsInRange_Int_WithinRange_DoesNotThrow()
    {
        var act = () => Ensure.That(5).IsInRange(1, 10);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void IsInRange_Int_OutOfRange_ThrowsGuardException(int value)
    {
        var act = () => Ensure.That(value).IsInRange(1, 10);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void AndReturn_ReturnsOriginalValue()
    {
        string result = Ensure.That("  hello  ").IsNotNullOrWhiteSpace().AndReturn();

        result.Should().Be("  hello  ");
    }

    [Fact]
    public void ChainedValidations_AllPass_DoesNotThrow()
    {
        var act = () => Ensure.That("hello").IsNotNullOrWhiteSpace().HasMinLength(3).HasMaxLength(10);

        act.Should().NotThrow();
    }

    [Fact]
    public void ChainedValidations_SecondFails_ThrowsGuardException()
    {
        var act = () => Ensure.That("hi").IsNotNullOrWhiteSpace().HasMinLength(3);

        act.Should().Throw<GuardException>();
    }

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user+tag@example.com")]
    [InlineData("user.name@example.co.uk")]
    public void IsValidEmail_ValidEmail_DoesNotThrow(string email)
    {
        var act = () => Ensure.That(email).IsValidEmail();

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("@missing-local.com")]
    [InlineData("missing-domain@")]
    public void IsValidEmail_InvalidEmail_ThrowsGuardException(string email)
    {
        var act = () => Ensure.That(email).IsValidEmail();

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Matches_MatchingPattern_DoesNotThrow()
    {
        var act = () => Ensure.That("Hello123").Matches("[A-Z]");

        act.Should().NotThrow();
    }

    [Fact]
    public void Matches_NonMatchingPattern_ThrowsGuardException()
    {
        var act = () => Ensure.That("hello").Matches("[0-9]");

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Matches_WithCustomMessage_ThrowsWithMessage()
    {
        var act = () => Ensure.That("hello").Matches("[0-9]", "Must contain a digit.");

        act.Should().Throw<GuardException>().WithMessage("Must contain a digit.");
    }

    [Fact]
    public void Matches_NullValue_ThrowsGuardException()
    {
        string? value = null;

        var act = () => Ensure.That(value!).Matches("[A-Z]");

        act.Should().Throw<GuardException>();
    }
}
