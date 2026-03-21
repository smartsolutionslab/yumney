using FluentAssertions;
using SmartSolutionsLab.Yumney.Shopping.Application.Requests;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Tests.Commands;

public class CreateShoppingListRequestValidatorTests
{
    private readonly CreateShoppingListRequestValidator validator = new();

    [Fact]
    public void Validate_ValidRequest_IsValid()
    {
        var request = CreateValidRequest();

        var result = validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_EmptyTitle_IsInvalid(string? title)
    {
        var request = CreateValidRequest() with { Title = title! };

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_TitleTooLong_IsInvalid()
    {
        var request = CreateValidRequest() with { Title = new string('a', 201) };

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_EmptyItems_IsInvalid()
    {
        var request = CreateValidRequest() with { Items = [] };

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_ItemNameEmpty_IsInvalid()
    {
        var request = CreateValidRequest() with
        {
            Items = [new CreateShoppingListItemRequest(string.Empty, null, null)],
        };

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_ItemNameTooLong_IsInvalid()
    {
        var request = CreateValidRequest() with
        {
            Items = [new CreateShoppingListItemRequest(new string('a', 201), null, null)],
        };

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_NegativeAmount_IsInvalid()
    {
        var request = CreateValidRequest() with
        {
            Items = [new CreateShoppingListItemRequest("Flour", -1, "g")],
        };

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_UnitTooLong_IsInvalid()
    {
        var request = CreateValidRequest() with
        {
            Items = [new CreateShoppingListItemRequest("Flour", 500, new string('a', 51))],
        };

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    private static CreateShoppingListRequest CreateValidRequest()
    {
        return new CreateShoppingListRequest(
            "Weekly Groceries",
            [new CreateShoppingListItemRequest("Flour", 500, "g")]);
    }
}
