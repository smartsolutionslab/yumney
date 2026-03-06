using FluentAssertions;
using Xunit;
using Yumney.Users.Application.Commands;

namespace Yumney.Users.Application.Tests.Commands;

public class RegisterUserCommandValidatorTests
{
    private readonly RegisterUserCommandValidator sut = new();

    [Fact]
    public void Validate_ValidCommand_IsValid()
    {
        var command = new RegisterUserCommand("test@example.com", "Password1", "Test User");

        var result = sut.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    public void Validate_InvalidEmail_IsNotValid(string email)
    {
        var command = new RegisterUserCommand(email, "Password1", "Test User");

        var result = sut.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Theory]
    [InlineData("short1A")]
    [InlineData("nouppercase1")]
    [InlineData("NOLOWERCASE1")]
    [InlineData("NoDigitsHere")]
    public void Validate_WeakPassword_IsNotValid(string password)
    {
        var command = new RegisterUserCommand("test@example.com", password, "Test User");

        var result = sut.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyDisplayName_IsNotValid(string displayName)
    {
        var command = new RegisterUserCommand("test@example.com", "Password1", displayName);

        var result = sut.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DisplayName");
    }
}
