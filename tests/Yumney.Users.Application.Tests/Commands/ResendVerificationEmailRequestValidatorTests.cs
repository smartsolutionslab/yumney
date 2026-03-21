using FluentAssertions;
using SmartSolutionsLab.Yumney.Users.Api.Requests;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Application.Tests.Commands;

public class ResendVerificationEmailRequestValidatorTests
{
    private readonly ResendVerificationEmailRequestValidator sut = new();

    [Fact]
    public void Validate_ValidRequest_IsValid()
    {
        var request = new ResendVerificationEmailRequest("test@example.com");

        var result = sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    public void Validate_InvalidEmail_IsNotValid(string email)
    {
        var request = new ResendVerificationEmailRequest(email);

        var result = sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Validate_EmailAtMaxLength_IsValid()
    {
        var localPart = new string('a', 242);
        var email = $"{localPart}@example.com";
        var request = new ResendVerificationEmailRequest(email);

        var result = sut.Validate(request);

        result.Errors.Should().NotContain(e => e.PropertyName == "Email" && e.ErrorCode == "MaximumLengthValidator");
    }

    [Fact]
    public void Validate_EmailExceedsMaxLength_IsNotValid()
    {
        var localPart = new string('a', 243);
        var email = $"{localPart}@example.com";
        var request = new ResendVerificationEmailRequest(email);

        var result = sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }
}
