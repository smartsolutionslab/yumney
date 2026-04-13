using FluentAssertions;
using FluentValidation.Results;
using SmartSolutionsLab.Yumney.Shared.Web;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Web;

public class ValidationExtensionsTests
{
    [Fact]
    public void HasFailed_InvalidResult_ReturnsTrue()
    {
        var result = new ValidationResult([new ValidationFailure("Name", "Required")]);

        result.HasFailed().Should().BeTrue();
    }

    [Fact]
    public void HasFailed_ValidResult_ReturnsFalse()
    {
        var result = new ValidationResult();

        result.HasFailed().Should().BeFalse();
    }

    [Fact]
    public void ToValidationProblem_ReturnsNonNullResult()
    {
        var result = new ValidationResult([new ValidationFailure("Name", "Required")]);

        var httpResult = result.ToValidationProblem();

        httpResult.Should().NotBeNull();
    }
}
