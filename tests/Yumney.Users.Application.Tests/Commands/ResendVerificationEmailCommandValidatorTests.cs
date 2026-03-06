using FluentAssertions;
using Xunit;
using Yumney.Users.Application.Commands;

namespace Yumney.Users.Application.Tests.Commands;

public class ResendVerificationEmailCommandValidatorTests
{
    private readonly ResendVerificationEmailCommandValidator sut = new();

    [Fact]
    public void Validate_ValidEmail_IsValid()
    {
        var command = new ResendVerificationEmailCommand("test@example.com");

        var result = sut.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    public void Validate_InvalidEmail_IsNotValid(string email)
    {
        var command = new ResendVerificationEmailCommand(email);

        var result = sut.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Validate_EmailExceedsMaxLength_IsNotValid()
    {
        var localPart = new string('a', 243);
        var email = $"{localPart}@example.com";
        var command = new ResendVerificationEmailCommand(email);

        var result = sut.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }
}
