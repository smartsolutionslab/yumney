using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Common;
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
}
