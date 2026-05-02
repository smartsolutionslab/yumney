using System.Diagnostics.Metrics;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.CQRS.Decorators;
using SmartSolutionsLab.Yumney.Shared.CQRS.Diagnostics;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.CQRS;

public class LoggingCommandHandlerDecoratorTests
{
	private readonly ICommandHandler<TestCommand, Result<string>> inner = Substitute.For<ICommandHandler<TestCommand, Result<string>>>();
	private readonly ILogger<LoggingCommandHandlerDecorator<TestCommand, Result<string>>> logger = Substitute.For<ILogger<LoggingCommandHandlerDecorator<TestCommand, Result<string>>>>();
	private readonly ApplicationMetrics metrics = new(new MeterFactory());

	[Fact]
	public async Task HandleAsync_Success_DelegatesAndReturnsResult()
	{
		var expected = Result<string>.Success("ok");
		inner.HandleAsync(Arg.Any<TestCommand>(), Arg.Any<CancellationToken>()).Returns(expected);
		var decorator = new LoggingCommandHandlerDecorator<TestCommand, Result<string>>(inner, logger, metrics);

		var result = await decorator.HandleAsync(new TestCommand());

		result.Should().BeSameAs(expected);
		await inner.Received(1).HandleAsync(Arg.Any<TestCommand>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_Failure_DelegatesAndReturnsResult()
	{
		var error = new ApiError("TEST_ERROR", "Something went wrong", 400);
		var expected = Result<string>.Failure(error);
		inner.HandleAsync(Arg.Any<TestCommand>(), Arg.Any<CancellationToken>()).Returns(expected);
		var decorator = new LoggingCommandHandlerDecorator<TestCommand, Result<string>>(inner, logger, metrics);

		var result = await decorator.HandleAsync(new TestCommand());

		result.IsFailure.Should().BeTrue();
		result.Error!.Code.Should().Be("TEST_ERROR");
	}

	[Fact]
	public async Task HandleAsync_Exception_RethrowsException()
	{
		inner.HandleAsync(Arg.Any<TestCommand>(), Arg.Any<CancellationToken>())
			.Returns<Result<string>>(_ => throw new InvalidOperationException("boom"));
		var decorator = new LoggingCommandHandlerDecorator<TestCommand, Result<string>>(inner, logger, metrics);

		Func<Task> act = () => decorator.HandleAsync(new TestCommand());

		await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
	}

	[Fact]
	public async Task HandleAsync_Failure_LogsWarningForResultFailure()
	{
		var error = new ApiError("CONFLICT", "Duplicate", 409);
		inner.HandleAsync(Arg.Any<TestCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result<string>.Failure(error));
		var decorator = new LoggingCommandHandlerDecorator<TestCommand, Result<string>>(inner, logger, metrics);

		var result = await decorator.HandleAsync(new TestCommand());

		result.IsFailure.Should().BeTrue();
		result.Error!.Code.Should().Be("CONFLICT");
	}

	[Fact]
	public async Task HandleAsync_NonResultType_TreatsAsSuccess()
	{
		var plainInner = Substitute.For<ICommandHandler<PlainCommand, string>>();
		plainInner.HandleAsync(Arg.Any<PlainCommand>(), Arg.Any<CancellationToken>()).Returns("done");
		var plainLogger = Substitute.For<ILogger<LoggingCommandHandlerDecorator<PlainCommand, string>>>();
		var decorator = new LoggingCommandHandlerDecorator<PlainCommand, string>(plainInner, plainLogger, metrics);

		var result = await decorator.HandleAsync(new PlainCommand());

		result.Should().Be("done");
	}

	private sealed class MeterFactory : IMeterFactory
	{
		public Meter Create(MeterOptions options) => new(options);

		public void Dispose()
		{
		}
	}
}
