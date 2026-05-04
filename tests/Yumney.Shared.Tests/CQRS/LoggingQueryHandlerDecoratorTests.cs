using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.CQRS.Decorators;
using SmartSolutionsLab.Yumney.Shared.CQRS.Diagnostics;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.CQRS;

public class LoggingQueryHandlerDecoratorTests
{
	private readonly IQueryHandler<TestQuery, Result<int>> inner = Substitute.For<IQueryHandler<TestQuery, Result<int>>>();
	private readonly ILogger<LoggingQueryHandlerDecorator<TestQuery, Result<int>>> logger = Substitute.For<ILogger<LoggingQueryHandlerDecorator<TestQuery, Result<int>>>>();
	private readonly ApplicationMetrics metrics = new(new TestMeterFactory());

	[Fact]
	public async Task HandleAsync_Success_DelegatesAndReturnsResult()
	{
		var expected = Result<int>.Success(42);
		inner.HandleAsync(Arg.Any<TestQuery>(), Arg.Any<CancellationToken>()).Returns(expected);
		var decorator = new LoggingQueryHandlerDecorator<TestQuery, Result<int>>(inner, logger, metrics);

		var result = await decorator.HandleAsync(new TestQuery());

		result.Should().BeSameAs(expected);
		await inner.Received(1).HandleAsync(Arg.Any<TestQuery>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_Failure_DelegatesAndReturnsResult()
	{
		var error = new ApiError("NOT_FOUND", "Not found", 404);
		var expected = Result<int>.Failure(error);
		inner.HandleAsync(Arg.Any<TestQuery>(), Arg.Any<CancellationToken>()).Returns(expected);
		var decorator = new LoggingQueryHandlerDecorator<TestQuery, Result<int>>(inner, logger, metrics);

		var result = await decorator.HandleAsync(new TestQuery());

		result.IsFailure.Should().BeTrue();
		result.Error!.Code.Should().Be("NOT_FOUND");
	}

	[Fact]
	public async Task HandleAsync_Exception_RethrowsException()
	{
		inner.HandleAsync(Arg.Any<TestQuery>(), Arg.Any<CancellationToken>())
			.Returns<Result<int>>(_ => throw new InvalidOperationException("boom"));
		var decorator = new LoggingQueryHandlerDecorator<TestQuery, Result<int>>(inner, logger, metrics);

		Func<Task> act = () => decorator.HandleAsync(new TestQuery());

		await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
	}
}
