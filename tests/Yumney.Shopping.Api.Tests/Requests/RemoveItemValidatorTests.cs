using FluentAssertions;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Api.Tests.Requests;

public class RemoveItemValidatorTests
{
	private readonly Api.Requests.Validator.RemoveItemValidator validator = new();

	[Fact]
	public void Validate_ValidRequest_IsValid()
	{
		var request = new Api.Requests.RemoveItem("Eggs", 6, "pc", "not needed");

		var result = validator.Validate(request);

		result.IsValid.Should().BeTrue();
	}

	[Fact]
	public void Validate_NameOnly_IsValid()
	{
		var request = new Api.Requests.RemoveItem("Eggs");

		var result = validator.Validate(request);

		result.IsValid.Should().BeTrue();
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	public void Validate_EmptyName_IsInvalid(string? name)
	{
		var request = new Api.Requests.RemoveItem(name!);

		var result = validator.Validate(request);

		result.IsValid.Should().BeFalse();
	}
}
