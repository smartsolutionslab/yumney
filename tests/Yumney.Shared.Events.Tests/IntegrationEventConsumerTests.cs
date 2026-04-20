using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Events.Wolverine;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Events.Tests;

public class IntegrationEventConsumerTests
{
	[Fact]
	public async Task HandleAsync_NewMessage_InvokesHandlerOnce()
	{
		var handler = Substitute.For<IIntegrationEventHandler<TestEvent>>();
		var inbox = new NoOpInboxStore();
		var consumer = CreateConsumer(handler, inbox);
		var message = new TestEvent();

		await consumer.HandleAsync(message, CancellationToken.None);

		await handler.Received(1).HandleAsync(message, Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_DuplicateMessage_SkipsHandler()
	{
		var handler = Substitute.For<IIntegrationEventHandler<TestEvent>>();
		var inbox = Substitute.For<IInboxStore>();
		inbox.TryMarkProcessedAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
			.Returns(false);

		var consumer = CreateConsumer(handler, inbox);
		var message = new TestEvent();

		await consumer.HandleAsync(message, CancellationToken.None);

		await handler.DidNotReceive().HandleAsync(Arg.Any<TestEvent>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_MultipleHandlers_GatesEachIndependently()
	{
		var handlerA = new RecordingHandler();
		var handlerB = new RecordingHandler();

		var inbox = Substitute.For<IInboxStore>();
		inbox.TryMarkProcessedAsync(Arg.Any<Guid>(), Arg.Is<string>(n => n.Contains(nameof(RecordingHandler))), Arg.Any<CancellationToken>())
			.Returns(true, false);

		var services = new ServiceCollection();
		services.AddSingleton<IIntegrationEventHandler<TestEvent>>(handlerA);
		services.AddSingleton<IIntegrationEventHandler<TestEvent>>(handlerB);
		var provider = services.BuildServiceProvider();

		var consumer = new IntegrationEventConsumer<TestEvent>(
			provider,
			inbox,
			NullLogger<IntegrationEventConsumer<TestEvent>>.Instance);

		await consumer.HandleAsync(new TestEvent(), CancellationToken.None);

		var totalCalls = handlerA.CallCount + handlerB.CallCount;
		totalCalls.Should().Be(1, "exactly one handler runs when the inbox allows one and blocks the other");
	}

	private sealed class RecordingHandler : IIntegrationEventHandler<TestEvent>
	{
		public int CallCount { get; private set; }

		public Task HandleAsync(TestEvent integrationEvent, CancellationToken cancellationToken = default)
		{
			CallCount++;
			return Task.CompletedTask;
		}
	}

	[Fact]
	public async Task HandleAsync_NoRegisteredHandlers_DoesNothing()
	{
		var inbox = Substitute.For<IInboxStore>();
		var provider = new ServiceCollection().BuildServiceProvider();

		var consumer = new IntegrationEventConsumer<TestEvent>(
			provider,
			inbox,
			NullLogger<IntegrationEventConsumer<TestEvent>>.Instance);

		await consumer.HandleAsync(new TestEvent(), CancellationToken.None);

		await inbox.DidNotReceive().TryMarkProcessedAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task NoOpInboxStore_AlwaysReturnsTrue()
	{
		var inbox = new NoOpInboxStore();

		var first = await inbox.TryMarkProcessedAsync(Guid.NewGuid(), "consumer");
		var second = await inbox.TryMarkProcessedAsync(Guid.NewGuid(), "consumer");

		first.Should().BeTrue();
		second.Should().BeTrue();
	}

	private static IntegrationEventConsumer<TestEvent> CreateConsumer(
		IIntegrationEventHandler<TestEvent> handler,
		IInboxStore inbox)
	{
		var services = new ServiceCollection();
		services.AddSingleton(handler);
		var provider = services.BuildServiceProvider();

		return new IntegrationEventConsumer<TestEvent>(
			provider,
			inbox,
			NullLogger<IntegrationEventConsumer<TestEvent>>.Instance);
	}

	public sealed record TestEvent : IntegrationEvent;
}
