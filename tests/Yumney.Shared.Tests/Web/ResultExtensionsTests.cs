using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shared.Web;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Web;

public class ResultExtensionsTests
{
	private static readonly ApiError TestError = new("NOT_FOUND", "Resource not found", 404);

	[Fact]
	public void ToOk_Success_ReturnsOkResult()
	{
		var success = Result<string>.Success("hello");

		var result = success.ToOk();

		var okResult = result as Ok<string>;
		okResult.Should().NotBeNull();
		okResult!.Value.Should().Be("hello");
	}

	[Fact]
	public void ToOk_Failure_ReturnsProblemResult()
	{
		var failure = Result<string>.Failure(TestError);

		var result = failure.ToOk();

		result.Should().BeOfType<ProblemHttpResult>();
	}

	[Fact]
	public void ToCreated_Success_ReturnsCreatedResult()
	{
		var success = Result<string>.Success("new-item");

		var result = success.ToCreated("/api/v1/items/1");

		var createdResult = result as Created<string>;
		createdResult.Should().NotBeNull();
		createdResult!.Value.Should().Be("new-item");
		createdResult.Location.Should().Be("/api/v1/items/1");
	}

	[Fact]
	public void ToCreated_Failure_ReturnsProblemResult()
	{
		var failure = Result<string>.Failure(TestError);

		var result = failure.ToCreated("/api/v1/items/1");

		result.Should().BeOfType<ProblemHttpResult>();
	}

	[Fact]
	public void ToNoContent_Success_ReturnsNoContentResult()
	{
		var success = Result.Success();

		var result = success.ToNoContent();

		result.Should().BeOfType<NoContent>();
	}

	[Fact]
	public void ToNoContent_Failure_ReturnsProblemResult()
	{
		var failure = Result.Failure(TestError);

		var result = failure.ToNoContent();

		result.Should().BeOfType<ProblemHttpResult>();
	}
}
