using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using SmartSolutionsLab.Yumney.Shared.Web.Validation;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Web;

public class ValidationExtensionsTests
{
    [Fact]
    public async Task ValidateAndProblemAsync_ValidRequest_ReturnsNull()
    {
        var validator = Substitute.For<IValidator<FakeRequest>>();
        validator.ValidateAsync(Arg.Any<FakeRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var result = await validator.ValidateAndProblemAsync(new FakeRequest("valid"));

        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAndProblemAsync_InvalidRequest_ReturnsValidationProblem()
    {
        var validator = Substitute.For<IValidator<FakeRequest>>();
        var failures = new List<ValidationFailure>
        {
            new("Name", "Name is required"),
        };
        validator.ValidateAsync(Arg.Any<FakeRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(failures));

        var result = await validator.ValidateAndProblemAsync(new FakeRequest(string.Empty));

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ValidateAndProblemAsync_ForwardsCancellationToken()
    {
        var validator = Substitute.For<IValidator<FakeRequest>>();
        validator.ValidateAsync(Arg.Any<FakeRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        var cts = new CancellationTokenSource();

        await validator.ValidateAndProblemAsync(new FakeRequest("test"), cts.Token);

        await validator.Received(1).ValidateAsync(Arg.Any<FakeRequest>(), cts.Token);
    }

    public sealed record FakeRequest(string Name);
}
