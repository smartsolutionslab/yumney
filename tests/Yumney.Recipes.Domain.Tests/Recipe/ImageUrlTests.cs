using FluentAssertions;
using Xunit;
using Yumney.Recipes.Domain.Recipe;
using Yumney.Shared.Guards;

namespace Yumney.Recipes.Domain.Tests.Recipe;

public class ImageUrlTests
{
    [Theory]
    [InlineData("http://example.com/image.jpg")]
    [InlineData("https://example.com/image.jpg")]
    public void Constructor_ValidUrl_CreatesInstance(string value)
    {
        var imageUrl = new ImageUrl(value);

        imageUrl.Value.Should().Be(value);
    }

    [Fact]
    public void Constructor_UrlWithQueryParams_CreatesInstance()
    {
        var value = "https://example.com/image.jpg?width=800";

        var imageUrl = new ImageUrl(value);

        imageUrl.Value.Should().Be(value);
    }

    [Fact]
    public void Constructor_TrimsWhitespace()
    {
        var imageUrl = new ImageUrl("  https://example.com/image.jpg  ");

        imageUrl.Value.Should().Be("https://example.com/image.jpg");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrWhitespace_ThrowsGuardException(string? value)
    {
        var act = () => new ImageUrl(value!);

        act.Should().Throw<GuardException>();
    }

    [Theory]
    [InlineData("ftp://example.com/image.jpg")]
    [InlineData("not-a-url")]
    [InlineData("example.com/image.jpg")]
    public void Constructor_InvalidFormat_ThrowsGuardException(string value)
    {
        var act = () => new ImageUrl(value);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Constructor_ExceedsMaxLength_ThrowsGuardException()
    {
        var path = new string('a', 2040);
        var url = $"https://x.com/{path}";

        var act = () => new ImageUrl(url);

        act.Should().Throw<GuardException>();
    }

    [Fact]
    public void Constructor_AtMaxLength_CreatesInstance()
    {
        var prefix = "https://x.com/";
        var path = new string('a', 2048 - prefix.Length);
        var url = $"{prefix}{path}";

        var result = new ImageUrl(url);

        result.Value.Should().HaveLength(2048);
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var url1 = new ImageUrl("https://example.com/image.jpg");
        var url2 = new ImageUrl("https://example.com/image.jpg");

        url1.Should().Be(url2);
    }

    [Fact]
    public void Equality_DifferentValue_AreNotEqual()
    {
        var url1 = new ImageUrl("https://example.com/image1.jpg");
        var url2 = new ImageUrl("https://example.com/image2.jpg");

        url1.Should().NotBe(url2);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var imageUrl = new ImageUrl("https://example.com/image.jpg");

        imageUrl.ToString().Should().Be("https://example.com/image.jpg");
    }
}
