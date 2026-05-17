using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Common;

public class ResultTests
{
	private static readonly ApiError TestError = new("TEST_ERROR", "Something went wrong", 500);
	private static readonly ApiError NotFoundError = new("NOT_FOUND", "Not found", 404);

	[Fact]
	public void Success_CreatesSuccessfulResult()
	{
		var result = Result.Success();

		result.IsSuccess.Should().BeTrue();
		result.IsFailure.Should().BeFalse();
		result.Error.Should().BeNull();
	}

	[Fact]
	public void Failure_CreatesFailedResult()
	{
		var result = Result.Failure(TestError);

		result.IsSuccess.Should().BeFalse();
		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(TestError);
	}

	[Fact]
	public void GenericSuccess_CreatesSuccessfulResultWithValue()
	{
		var result = Result<int>.Success(42);

		result.IsSuccess.Should().BeTrue();
		result.Value.Should().Be(42);
	}

	[Fact]
	public void GenericFailure_CreatesFailedResult()
	{
		var result = Result<int>.Failure(NotFoundError);

		result.IsSuccess.Should().BeFalse();
		result.Error.Should().Be(NotFoundError);
	}

	[Fact]
	public void GenericFailure_AccessingValue_ThrowsInvalidOperationException()
	{
		var result = Result<int>.Failure(NotFoundError);

		var act = () => result.Value;

		act.Should().Throw<InvalidOperationException>();
	}

	[Fact]
	public void Failure_WithNullError_ThrowsInvalidOperationException()
	{
		var act = () => Result.Failure(null!);

		act.Should().Throw<InvalidOperationException>();
	}

	[Fact]
	public void BaseSuccessOfT_CreatesSuccessfulResultWithValue()
	{
		// Result.Success<T>(value) is the static factory on the base class —
		// equivalent to Result<T>.Success(value) but reachable without the
		// generic at the call site.
		var result = Result.Success("hello");

		result.IsSuccess.Should().BeTrue();
		result.Value.Should().Be("hello");
	}

	[Fact]
	public void BaseSuccessOfT_WithNullValue_ThrowsArgumentNullException()
	{
		var act = () => Result.Success<string>(null!);

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void GenericSuccess_WithNullValue_ThrowsArgumentNullException()
	{
		var act = () => Result<string>.Success(null!);

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void BaseFailureOfT_CreatesFailedTypedResult()
	{
		// Result.Failure<T>(error) — typed failure factory on the base class.
		var result = Result.Failure<int>(TestError);

		result.IsSuccess.Should().BeFalse();
		result.Error.Should().Be(TestError);
	}

	[Fact]
	public void ImplicitOperator_FromValue_ProducesSuccess()
	{
		Result<int> result = 42;

		result.IsSuccess.Should().BeTrue();
		result.Value.Should().Be(42);
	}

	[Fact]
	public void ImplicitOperator_FromApiError_ProducesFailure()
	{
		Result<int> result = TestError;

		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(TestError);
	}

	[Fact]
	public void GenericFailure_WithNullError_ThrowsInvalidOperationException()
	{
		// Reaches the "failed result must have an error" guard inside the
		// shared protected ctor via the typed Failure helper.
		var act = () => Result.Failure<int>(null!);

		act.Should().Throw<InvalidOperationException>();
	}
}
