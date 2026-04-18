using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS.Decorators;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.CQRS;

public class ResultInspectorTests
{
	[Fact]
	public void IsFailure_SuccessResult_ReturnsFalse()
	{
		var result = Result<string>.Success("ok");

		var isFailure = ResultInspector.IsFailure(result, out var code, out var message);

		isFailure.Should().BeFalse();
		code.Should().BeNull();
		message.Should().BeNull();
	}

	[Fact]
	public void IsFailure_FailedResult_ReturnsTrueWithErrorDetails()
	{
		var error = new ApiError("DUPLICATE", "Already exists", 409);
		var result = Result<string>.Failure(error);

		var isFailure = ResultInspector.IsFailure(result, out var code, out var message);

		isFailure.Should().BeTrue();
		code.Should().Be("DUPLICATE");
		message.Should().Be("Already exists");
	}

	[Fact]
	public void IsFailure_NonGenericResult_Success_ReturnsFalse()
	{
		var result = Result.Success();

		var isFailure = ResultInspector.IsFailure(result, out _, out _);

		isFailure.Should().BeFalse();
	}

	[Fact]
	public void IsFailure_NonGenericResult_Failure_ReturnsTrueWithErrorDetails()
	{
		var error = new ApiError("FORBIDDEN", "Access denied", 403);
		var result = Result.Failure(error);

		var isFailure = ResultInspector.IsFailure(result, out var code, out var message);

		isFailure.Should().BeTrue();
		code.Should().Be("FORBIDDEN");
		message.Should().Be("Access denied");
	}

	[Fact]
	public void IsFailure_NonResultType_ReturnsFalse()
	{
		var isFailure = ResultInspector.IsFailure("just a string", out var code, out var message);

		isFailure.Should().BeFalse();
		code.Should().BeNull();
		message.Should().BeNull();
	}

	[Fact]
	public void IsFailure_Null_ReturnsFalse()
	{
		var isFailure = ResultInspector.IsFailure<string?>(null, out _, out _);

		isFailure.Should().BeFalse();
	}
}
