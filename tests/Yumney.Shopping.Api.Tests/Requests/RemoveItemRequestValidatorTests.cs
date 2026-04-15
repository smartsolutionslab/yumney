using FluentAssertions;
using SmartSolutionsLab.Yumney.Shopping.Api.Requests;
using SmartSolutionsLab.Yumney.Shopping.Api.Requests.Validator;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Api.Tests.Requests;

public class RemoveItemRequestValidatorTests
{
    private readonly RemoveItemRequestValidator validator = new();

    [Fact]
    public void Validate_ValidRequest_IsValid()
    {
        var request = new RemoveItemRequest("Eggs", 6, "pc", "not needed");

        var result = validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_NameOnly_IsValid()
    {
        var request = new RemoveItemRequest("Eggs");

        var result = validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_EmptyName_IsInvalid(string? name)
    {
        var request = new RemoveItemRequest(name!);

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }
}
