using FluentValidation.TestHelper;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Api.Tests.Requests.Validator;

public class SaveRecipeIngredientValidatorTests
{
	private readonly Api.Requests.Validator.SaveRecipeIngredientValidator validator = new();

	[Fact]
	public void Validate_ValidRequest_HasNoErrors()
	{
		var request = new Api.Requests.SaveRecipeIngredient("Flour", 500, "g");

		var result = validator.TestValidate(request);

		result.ShouldNotHaveAnyValidationErrors();
	}

	[Fact]
	public void Validate_NullAmountAndUnit_HasNoErrors()
	{
		var request = new Api.Requests.SaveRecipeIngredient("Salt", null, null);

		var result = validator.TestValidate(request);

		result.ShouldNotHaveAnyValidationErrors();
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void Validate_EmptyName_HasError(string? name)
	{
		var request = new Api.Requests.SaveRecipeIngredient(name!, 100, "g");

		var result = validator.TestValidate(request);

		result.ShouldHaveValidationErrorFor(x => x.Name);
	}

	[Fact]
	public void Validate_NameExceedsMaxLength_HasError()
	{
		var request = new Api.Requests.SaveRecipeIngredient(new string('a', IngredientName.MaxLength + 1), 100, "g");

		var result = validator.TestValidate(request);

		result.ShouldHaveValidationErrorFor(x => x.Name);
	}

	[Fact]
	public void Validate_NameAtMaxLength_HasNoError()
	{
		var request = new Api.Requests.SaveRecipeIngredient(new string('a', IngredientName.MaxLength), 100, "g");

		var result = validator.TestValidate(request);

		result.ShouldNotHaveValidationErrorFor(x => x.Name);
	}

	[Fact]
	public void Validate_NegativeAmount_HasError()
	{
		var request = new Api.Requests.SaveRecipeIngredient("Flour", -1, "g");

		var result = validator.TestValidate(request);

		result.ShouldHaveValidationErrorFor(x => x.Amount);
	}

	[Fact]
	public void Validate_ZeroAmount_HasNoError()
	{
		var request = new Api.Requests.SaveRecipeIngredient("Flour", 0, "g");

		var result = validator.TestValidate(request);

		result.ShouldNotHaveValidationErrorFor(x => x.Amount);
	}

	[Fact]
	public void Validate_PositiveAmount_HasNoError()
	{
		var request = new Api.Requests.SaveRecipeIngredient("Flour", 500, "g");

		var result = validator.TestValidate(request);

		result.ShouldNotHaveValidationErrorFor(x => x.Amount);
	}

	[Fact]
	public void Validate_NullAmount_HasNoError()
	{
		var request = new Api.Requests.SaveRecipeIngredient("Flour", null, "g");

		var result = validator.TestValidate(request);

		result.ShouldNotHaveValidationErrorFor(x => x.Amount);
	}

	[Fact]
	public void Validate_UnitExceedsMaxLength_HasError()
	{
		var request = new Api.Requests.SaveRecipeIngredient("Flour", 500, new string('a', Unit.MaxLength + 1));

		var result = validator.TestValidate(request);

		result.ShouldHaveValidationErrorFor(x => x.Unit);
	}

	[Fact]
	public void Validate_UnitAtMaxLength_HasNoError()
	{
		var request = new Api.Requests.SaveRecipeIngredient("Flour", 500, new string('a', Unit.MaxLength));

		var result = validator.TestValidate(request);

		result.ShouldNotHaveValidationErrorFor(x => x.Unit);
	}

	[Fact]
	public void Validate_NullUnit_HasNoError()
	{
		var request = new Api.Requests.SaveRecipeIngredient("Flour", 500, null);

		var result = validator.TestValidate(request);

		result.ShouldNotHaveValidationErrorFor(x => x.Unit);
	}
}
