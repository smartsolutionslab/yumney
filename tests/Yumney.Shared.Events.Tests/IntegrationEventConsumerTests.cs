using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Events.Wolverine;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Events.Tests;

public class IntegrationEventConsumerTests
{
	[Fact]
	public async Task HandleAsync_NewMessage_InvokesHandler()
	{
		var handler = new RecordingHandler();
		var inbox = new TestInboxStore();
		var consumer = CreateConsumer(handler, inbox);

		await consumer.HandleAsync(new TestEvent(), CancellationToken.None);

		handler.CallCount.Should().Be(1);
		inbox.Invocations.Should().HaveCount(1);
		inbox.Invocations[0].HandlerCompleted.Should().BeTrue();
	}

	[Fact]
	public async Task HandleAsync_DuplicateMessage_SkipsHandler()
	{
		var handler = new RecordingHandler();
		var inbox = new TestInboxStore(shouldProcessSequence: [false]);
		var consumer = CreateConsumer(handler, inbox);

		await consumer.HandleAsync(new TestEvent(), CancellationToken.None);

		handler.CallCount.Should().Be(0);
		inbox.Invocations[0].ShouldProcess.Should().BeFalse();
		inbox.Invocations[0].HandlerCompleted.Should().BeFalse();
	}

	[Fact]
	public async Task HandleAsync_HandlerThrows_PropagatesException()
	{
		var handler = new RecordingHandler { ThrowOnCall = new InvalidOperationException("boom") };
		var inbox = new TestInboxStore();
		var consumer = CreateConsumer(handler, inbox);

		var act = async () => await consumer.HandleAsync(new TestEvent(), CancellationToken.None);

		await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
		inbox.Invocations[0].HandlerCompleted.Should().BeFalse();
	}

	[Fact]
	public async Task HandleAsync_DuplicateRaceOnHandlerWrite_SwallowsAndContinues()
	{
		var raceException = new InvalidOperationException("unique violation");
		var handler = new RecordingHandler { ThrowOnCall = raceException };
		var inbox = new TestInboxStore(isDuplicate: ex => ReferenceEquals(ex, raceException));
		var consumer = CreateConsumer(handler, inbox);

		await consumer.HandleAsync(new TestEvent(), CancellationToken.None);

		inbox.Invocations[0].DuplicateRace.Should().BeTrue();
		inbox.Invocations[0].HandlerCompleted.Should().BeFalse();
	}

	[Fact]
	public async Task HandleAsync_MultipleHandlers_GatesEachIndependently()
	{
		var handlerA = new RecordingHandler();
		var handlerB = new RecordingHandler();
		var inbox = new TestInboxStore(shouldProcessSequence: [true, false]);

		var services = new ServiceCollection();
		services.AddSingleton<IIntegrationEventHandler<TestEvent>>(handlerA);
		services.AddSingleton<IIntegrationEventHandler<TestEvent>>(handlerB);
		var provider = services.BuildServiceProvider();

		var consumer = new IntegrationEventConsumer<TestEvent>(
			provider,
			inbox,
			NullLogger<IntegrationEventConsumer<TestEvent>>.Instance);

		await consumer.HandleAsync(new TestEvent(), CancellationToken.None);

		(handlerA.CallCount + handlerB.CallCount).Should().Be(1);
		inbox.Invocations.Should().HaveCount(2);
		inbox.Invocations.Count(invocation => invocation.HandlerCompleted).Should().Be(1);
	}

	[Fact]
	public async Task HandleAsync_NoRegisteredHandlers_DoesNothing()
	{
		var inbox = new TestInboxStore();
		var provider = new ServiceCollection().BuildServiceProvider();

		var consumer = new IntegrationEventConsumer<TestEvent>(
			provider,
			inbox,
			NullLogger<IntegrationEventConsumer<TestEvent>>.Instance);

		await consumer.HandleAsync(new TestEvent(), CancellationToken.None);

		inbox.Invocations.Should().BeEmpty();
	}

	[Fact]
	public async Task NoOpInboxStore_AlwaysRunsHandler()
	{
		var inbox = new NoOpInboxStore();
		var calls = 0;

		var firstRan = await inbox.ProcessAsync(Guid.NewGuid(), "consumer", () =>
		{
			calls++;
			return Task.CompletedTask;
		});
		var secondRan = await inbox.ProcessAsync(Guid.NewGuid(), "consumer", () =>
		{
			calls++;
			return Task.CompletedTask;
		});

		firstRan.Should().BeTrue();
		secondRan.Should().BeTrue();
		calls.Should().Be(2);
	}

	private sealed class RecordingHandler : IIntegrationEventHandler<TestEvent>
	{
		public int CallCount { get; private set; }

		public Exception? ThrowOnCall { get; init; }

		public Task HandleAsync(TestEvent integrationEvent, CancellationToken cancellationToken = default)
		{
			CallCount++;
			if (ThrowOnCall is not null) throw ThrowOnCall;
			return Task.CompletedTask;
		}
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
