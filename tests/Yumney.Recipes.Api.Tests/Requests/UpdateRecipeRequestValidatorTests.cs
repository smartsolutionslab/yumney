using FluentAssertions;
using FluentValidation.TestHelper;
using SmartSolutionsLab.Yumney.Recipes.Api.Requests;
using SmartSolutionsLab.Yumney.Recipes.Api.Requests.Validator;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Api.Tests.Requests;

public class UpdateRecipeRequestValidatorTests
{
    private readonly UpdateRecipeRequestValidator validator = new();

    [Fact]
    public void Validate_ValidRequest_HasNoErrors()
    {
        var request = CreateValidRequest();

        var result = validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyTitle_HasError(string? title)
    {
        var request = CreateValidRequest() with { Title = title! };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Validate_TitleExceedsMaxLength_HasError()
    {
        var request = CreateValidRequest() with { Title = new string('a', 201) };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Validate_TitleAtMaxLength_HasNoError()
    {
        var request = CreateValidRequest() with { Title = new string('a', 200) };

        var result = validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Validate_EmptyIngredients_HasError()
    {
        var request = CreateValidRequest() with { Ingredients = [] };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Ingredients);
    }

    [Fact]
    public void Validate_IngredientWithEmptyName_HasError()
    {
        var request = CreateValidRequest() with
        {
            Ingredients = [new SaveRecipeIngredientRequest(string.Empty, null, null)],
        };

        var result = validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_EmptySteps_HasError()
    {
        var request = CreateValidRequest() with { Steps = [] };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Steps);
    }

    [Fact]
    public void Validate_StepWithEmptyDescription_HasError()
    {
        var request = CreateValidRequest() with
        {
            Steps = [new SaveRecipeStepRequest(1, string.Empty)],
        };

        var result = validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_NullOptionalFields_HasNoErrors()
    {
        var request = CreateValidRequest() with
        {
            Description = null,
            Servings = null,
            PrepTimeMinutes = null,
            CookTimeMinutes = null,
            Difficulty = null,
            ImageUrl = null,
        };

        var result = validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ZeroServings_HasError()
    {
        var request = CreateValidRequest() with { Servings = 0 };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Servings);
    }

    [Fact]
    public void Validate_NegativeServings_HasError()
    {
        var request = CreateValidRequest() with { Servings = -1 };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Servings);
    }

    [Fact]
    public void Validate_PositiveServings_HasNoError()
    {
        var request = CreateValidRequest() with { Servings = 1 };

        var result = validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.Servings);
    }

    [Fact]
    public void Validate_NegativePrepTime_HasError()
    {
        var request = CreateValidRequest() with { PrepTimeMinutes = -1 };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.PrepTimeMinutes);
    }

    [Fact]
    public void Validate_ZeroPrepTime_HasNoError()
    {
        var request = CreateValidRequest() with { PrepTimeMinutes = 0 };

        var result = validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.PrepTimeMinutes);
    }

    [Fact]
    public void Validate_NegativeCookTime_HasError()
    {
        var request = CreateValidRequest() with { CookTimeMinutes = -1 };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.CookTimeMinutes);
    }

    [Fact]
    public void Validate_ZeroCookTime_HasNoError()
    {
        var request = CreateValidRequest() with { CookTimeMinutes = 0 };

        var result = validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.CookTimeMinutes);
    }

    [Fact]
    public void Validate_DescriptionExceedsMaxLength_HasError()
    {
        var request = CreateValidRequest() with { Description = new string('a', 2001) };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_DescriptionAtMaxLength_HasNoError()
    {
        var request = CreateValidRequest() with { Description = new string('a', 2000) };

        var result = validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_DifficultyExceedsMaxLength_HasError()
    {
        var request = CreateValidRequest() with { Difficulty = new string('a', 51) };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Difficulty);
    }

    [Fact]
    public void Validate_EmptyDifficulty_HasError()
    {
        var request = CreateValidRequest() with { Difficulty = string.Empty };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Difficulty);
    }

    [Fact]
    public void Validate_InvalidImageUrl_HasError()
    {
        var request = CreateValidRequest() with { ImageUrl = "not-a-url" };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.ImageUrl);
    }

    [Fact]
    public void Validate_ValidImageUrl_HasNoError()
    {
        var request = CreateValidRequest() with { ImageUrl = "https://example.com/image.jpg" };

        var result = validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.ImageUrl);
    }

    [Fact]
    public void Validate_ImageUrlExceedsMaxLength_HasError()
    {
        var request = CreateValidRequest() with
        {
            ImageUrl = "https://example.com/" + new string('a', 2040),
        };

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.ImageUrl);
    }

    [Fact]
    public void Validate_IngredientNameExceedsMaxLength_HasError()
    {
        var request = CreateValidRequest() with
        {
            Ingredients = [new SaveRecipeIngredientRequest(new string('a', 201), null, null)],
        };

        var result = validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_NegativeIngredientAmount_HasError()
    {
        var request = CreateValidRequest() with
        {
            Ingredients = [new SaveRecipeIngredientRequest("Flour", -1, "g")],
        };

        var result = validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_ZeroIngredientAmount_HasNoError()
    {
        var request = CreateValidRequest() with
        {
            Ingredients = [new SaveRecipeIngredientRequest("Flour", 0, "g")],
        };

        var result = validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_IngredientWithNullAmountAndUnit_HasNoError()
    {
        var request = CreateValidRequest() with
        {
            Ingredients = [new SaveRecipeIngredientRequest("Salt", null, null)],
        };

        var result = validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_IngredientUnitExceedsMaxLength_HasError()
    {
        var request = CreateValidRequest() with
        {
            Ingredients = [new SaveRecipeIngredientRequest("Flour", 500, new string('a', 51))],
        };

        var result = validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_StepDescriptionExceedsMaxLength_HasError()
    {
        var request = CreateValidRequest() with
        {
            Steps = [new SaveRecipeStepRequest(1, new string('a', 2001))],
        };

        var result = validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_ZeroStepNumber_HasError()
    {
        var request = CreateValidRequest() with
        {
            Steps = [new SaveRecipeStepRequest(0, "Cook pasta")],
        };

        var result = validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_NegativeStepNumber_HasError()
    {
        var request = CreateValidRequest() with
        {
            Steps = [new SaveRecipeStepRequest(-1, "Cook pasta")],
        };

        var result = validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
    }

    private static UpdateRecipeRequest CreateValidRequest()
    {
        return new UpdateRecipeRequest(
            "Pasta Carbonara",
            "A classic dish",
            [new SaveRecipeIngredientRequest("Spaghetti", 400, "g")],
            [new SaveRecipeStepRequest(1, "Cook pasta")],
            4,
            10,
            20,
            "medium",
            null);
    }
}
