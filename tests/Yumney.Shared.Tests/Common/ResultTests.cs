using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Common;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Common;

public class ResultTests
{
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
        var result = Result.Failure("Something went wrong");

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Something went wrong");
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
        var result = Result<int>.Failure("Not found");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Not found");
    }

    [Fact]
    public void GenericFailure_AccessingValue_ThrowsInvalidOperationException()
    {
        var result = Result<int>.Failure("Not found");

        var act = () => result.Value;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Failure_WithEmptyError_ThrowsInvalidOperationException()
    {
        var act = () => Result.Failure(string.Empty);

        act.Should().Throw<InvalidOperationException>();
    }
}
