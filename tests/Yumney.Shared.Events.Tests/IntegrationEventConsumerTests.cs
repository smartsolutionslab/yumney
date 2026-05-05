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
	public async Task HandleAsync_NewMessage_InvokesHandlerAndRecordsProcessedOutcome()
	{
		var handler = new RecordingHandler();
		var inbox = new TestInboxStore();
		var consumer = CreateConsumer(handler, inbox);

		await consumer.HandleAsync(new TestEvent(), CancellationToken.None);

		handler.CallCount.Should().Be(1);
		inbox.Invocations.Should().HaveCount(1);
		inbox.Invocations[0].Outcome.Should().Be(InboxOutcome.Processed);
		inbox.Invocations[0].HandlerInvoked.Should().BeTrue();
	}

	[Fact]
	public async Task HandleAsync_AlreadyProcessed_SkipsHandlerWithoutInvoking()
	{
		var handler = new RecordingHandler();
		var inbox = new TestInboxStore(outcomeSequence: [InboxOutcome.AlreadyProcessed]);
		var consumer = CreateConsumer(handler, inbox);

		await consumer.HandleAsync(new TestEvent(), CancellationToken.None);

		handler.CallCount.Should().Be(0);
		inbox.Invocations[0].HandlerInvoked.Should().BeFalse();
	}

	[Fact]
	public async Task HandleAsync_HandlerThrows_PropagatesAfterStoreRollsBack()
	{
		var handler = new RecordingHandler { ThrowOnCall = new InvalidOperationException("boom") };
		var inbox = new TestInboxStore();
		var consumer = CreateConsumer(handler, inbox);

		var act = async () => await consumer.HandleAsync(new TestEvent(), CancellationToken.None);

		await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
		inbox.Invocations[0].HandlerInvoked.Should().BeTrue();
		inbox.Invocations[0].HandlerThrew.Should().BeTrue();
	}

	[Fact]
	public async Task HandleAsync_DuplicateRace_HandlerRanButOutcomeIsRolledBack()
	{
		var handler = new RecordingHandler();
		var inbox = new TestInboxStore(outcomeSequence: [InboxOutcome.DuplicateRace]);
		var consumer = CreateConsumer(handler, inbox);

		await consumer.HandleAsync(new TestEvent(), CancellationToken.None);

		// The store invokes the handler then reports the commit failed; the
		// consumer must NOT rethrow — DuplicateRace is a known/expected race.
		handler.CallCount.Should().Be(1);
		inbox.Invocations[0].Outcome.Should().Be(InboxOutcome.DuplicateRace);
	}

	[Fact]
	public async Task HandleAsync_MultipleHandlers_GatesEachIndependently()
	{
		var handlerA = new RecordingHandler();
		var handlerB = new RecordingHandler();
		var inbox = new TestInboxStore(outcomeSequence: [InboxOutcome.Processed, InboxOutcome.AlreadyProcessed]);

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
		inbox.Invocations.Count(invocation => invocation.HandlerInvoked).Should().Be(1);
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
	public async Task NoOpInboxStore_AlwaysInvokesHandlerAndReportsProcessed()
	{
		var inbox = new NoOpInboxStore();
		var handlerCalls = 0;

		var first = await inbox.TryProcessAsync(Guid.NewGuid(), "consumer", _ =>
		{
			handlerCalls++;
			return Task.CompletedTask;
		});
		var second = await inbox.TryProcessAsync(Guid.NewGuid(), "consumer", _ =>
		{
			handlerCalls++;
			return Task.CompletedTask;
		});

		first.Should().Be(InboxOutcome.Processed);
		second.Should().Be(InboxOutcome.Processed);
		handlerCalls.Should().Be(2);
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
