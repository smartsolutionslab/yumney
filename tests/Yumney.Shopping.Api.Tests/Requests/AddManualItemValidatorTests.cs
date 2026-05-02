using FluentAssertions;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Api.Tests.Requests;

public class AddManualItemValidatorTests
{
	private readonly Api.Requests.Validator.AddManualItemValidator validator = new();

	[Fact]
	public void Validate_ValidRequest_IsValid()
	{
		var request = new Api.Requests.AddManualItem("Milk", 2, "L");

		var result = validator.Validate(request);

		result.IsValid.Should().BeTrue();
	}

	[Fact]
	public void Validate_NameOnly_IsValid()
	{
		var request = new Api.Requests.AddManualItem("Milk");

		var result = validator.Validate(request);

		result.IsValid.Should().BeTrue();
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	public void Validate_EmptyName_IsInvalid(string? name)
	{
		var request = new Api.Requests.AddManualItem(name!);

		var result = validator.Validate(request);

		result.IsValid.Should().BeFalse();
	}
}
