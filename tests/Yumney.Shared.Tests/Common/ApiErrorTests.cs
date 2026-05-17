using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Common;

public class ApiErrorTests
{
	[Fact]
	public void Constructor_SetsAllProperties()
	{
		var error = new ApiError("NOT_FOUND", "Resource not found", 404);

		error.Code.Should().Be("NOT_FOUND");
		error.Message.Should().Be("Resource not found");
		error.HttpStatusCode.Should().Be(404);
	}

	[Fact]
	public void Equality_SameValues_AreEqual()
	{
		var error1 = new ApiError("NOT_FOUND", "Resource not found", 404);
		var error2 = new ApiError("NOT_FOUND", "Resource not found", 404);

		error1.Should().Be(error2);
	}

	[Fact]
	public void Equality_DifferentCode_AreNotEqual()
	{
		var error1 = new ApiError("NOT_FOUND", "Resource not found", 404);
		var error2 = new ApiError("BAD_REQUEST", "Resource not found", 404);

		error1.Should().NotBe(error2);
	}

	[Fact]
	public void Equality_DifferentMessage_AreNotEqual()
	{
		var error1 = new ApiError("NOT_FOUND", "Resource not found", 404);
		var error2 = new ApiError("NOT_FOUND", "Different message", 404);

		error1.Should().NotBe(error2);
	}

	[Fact]
	public void Equality_DifferentHttpStatusCode_AreNotEqual()
	{
		var error1 = new ApiError("NOT_FOUND", "Resource not found", 404);
		var error2 = new ApiError("NOT_FOUND", "Resource not found", 410);

		error1.Should().NotBe(error2);
	}

	[Fact]
	public void GetHashCode_SameValues_AreEqual()
	{
		var error1 = new ApiError("NOT_FOUND", "Resource not found", 404);
		var error2 = new ApiError("NOT_FOUND", "Resource not found", 404);

		error1.GetHashCode().Should().Be(error2.GetHashCode());
	}

	[Fact]
	public void ToString_ReturnsPropertyValues()
	{
		var error = new ApiError("NOT_FOUND", "Resource not found", 404);

		var text = error.ToString();

		text.Should().Contain("NOT_FOUND").And.Contain("Resource not found").And.Contain("404");
	}

	[Fact]
	public void With_Expression_CreatesNewInstanceWithModifiedProperty()
	{
		var original = new ApiError("NOT_FOUND", "Resource not found", 404);

		var modified = original with { Message = "Different message" };

		modified.Code.Should().Be("NOT_FOUND");
		modified.Message.Should().Be("Different message");
		modified.HttpStatusCode.Should().Be(404);
		original.Message.Should().Be("Resource not found");
	}

	[Fact]
	public void Deconstruct_YieldsAllProperties()
	{
		var error = new ApiError("NOT_FOUND", "Resource not found", 404);

		var (code, message, httpStatusCode) = error;

		code.Should().Be("NOT_FOUND");
		message.Should().Be("Resource not found");
		httpStatusCode.Should().Be(404);
	}
}
